using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharedHosting.Telemetry;
using SharedInfrastructure.Constants;
using SharedInfrastructure.EventBus;
using SharedInfrastructure.Inbox;
using SharedInfrastructure.Telemetry;
using SharedKernel.Entities;
using SharedKernel.Json;

namespace SharedInfrastructure.Outbox;

/// <summary>
/// Фоновый сервис для публикации сообщений из Outbox в Kafka
/// </summary>
public class OutboxPublisher<TContext>(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxPublisher<TContext>> logger)
    : BackgroundService
    where TContext : DbContext
{
    private readonly int _batchSize = 200;
    private const int MaxRetryAttempts = 5;
    private const int MaxDegreeOfParallelism = 10;

    private readonly TimeSpan _minInterval = TimeSpan.FromMilliseconds(10);
    private readonly TimeSpan _maxInterval = TimeSpan.FromMilliseconds(500);
    private TimeSpan _currentInterval = TimeSpan.FromMilliseconds(100);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox Publisher started");

        while (!stoppingToken.IsCancellationRequested)
        {
            bool hasWork;
            try
            {
                hasWork = await PublishPendingMessages(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error publishing outbox messages");
                hasWork = true;
            }

            _currentInterval = hasWork 
                ? _minInterval 
                : TimeSpan.FromTicks(Math.Min(_currentInterval.Ticks * 2, _maxInterval.Ticks));

            await Task.Delay(_currentInterval, stoppingToken);
        }

        logger.LogInformation("Outbox Publisher stopped");
    }

    private async Task<bool> PublishPendingMessages(CancellationToken cancellationToken)
    {
        List<OutboxMessage> messages;
        IEventBus eventBus;
        IDeadLetterQueue deadLetterQueue;

        using (var scope = scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TContext>();
            eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
            deadLetterQueue = scope.ServiceProvider.GetRequiredService<IDeadLetterQueue>();

            messages = await context.Set<OutboxMessage>()
                .Where(x => x.ProcessedAt == null && x.ProcessingAttempts < MaxRetryAttempts)
                .OrderBy(x => x.CreatedAt)
                .Take(_batchSize)
                .ToListAsync(cancellationToken);
        }

        if (messages.Count == 0)
            return false;

        logger.LogInformation("Found {Count} pending outbox messages", messages.Count);

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = MaxDegreeOfParallelism,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(messages, parallelOptions, async (message, ct) =>
        {
            await PublishSingleMessageAsync(message, eventBus, deadLetterQueue, ct);
        });

        using (var scope = scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TContext>();

            foreach (var message in messages)
            {
                var entry = context.Entry(message);
                if (entry.State == EntityState.Detached)
                    context.Attach(message);
                entry.State = EntityState.Modified;
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    private async Task PublishSingleMessageAsync(
        OutboxMessage message,
        IEventBus eventBus,
        IDeadLetterQueue deadLetterQueue,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!string.IsNullOrEmpty(message.TraceParent))
                TraceContextCarrier.TraceParent = message.TraceParent;

            var eventType = EventTypeRegistry.GetType(message.EventType);
            if (eventType == null)
            {
                logger.LogWarning("Unknown event type: {EventType}", message.EventType);
                await MoveToDeadLetterQueueAsync(message,
                    new Exception($"Unknown event type: {message.EventType}"),
                    deadLetterQueue, cancellationToken);
                return;
            }

            var @event = JsonSerializer.Deserialize(message.Payload, eventType, JsonDefaults.Options);
            if (@event == null)
            {
                logger.LogWarning("Failed to deserialize event: {EventType}", message.EventType);
                await MoveToDeadLetterQueueAsync(message,
                    new Exception($"Failed to deserialize event: {message.EventType}"),
                    deadLetterQueue, cancellationToken);
                return;
            }

            var eventTypeShort = GetSimpleTypeName(message.EventType);

            if (TelemetryConstants.ActivitySource != null)
            {
                using var activity = ActivityBuilder.RestoreAndCreateActivity(
                        $"{TelemetrySpanNames.OutboxPublish} {eventTypeShort}",
                        message.TraceParent,
                        ActivityKind.Producer)
                    .WithOutboxPublisherTags(
                        message.EventType,
                        eventTypeShort,
                        message.AggregateId,
                        message.AggregateType,
                        message.Id,
                        message.ProcessingAttempts);

                var publisher = EventTypeRegistry.GetPublisher(eventType);
                await publisher(eventBus, @event, cancellationToken);
            }
            else
            {
                var publisher = EventTypeRegistry.GetPublisher(eventType);
                await publisher(eventBus, @event, cancellationToken);
            }

            message.ProcessedAt = DateTime.UtcNow;
            message.ErrorMessage = null;
            logger.LogDebug("Published outbox message {MessageId} of type {EventType}",
                message.Id, message.EventType);
        }
        catch (Exception ex)
        {
            message.ProcessingAttempts++;
            message.ErrorMessage = ex.Message;

            logger.LogError(ex, "Failed to publish outbox message {MessageId}, attempt {Attempts}",
                message.Id, message.ProcessingAttempts);

            if (message.ProcessingAttempts >= MaxRetryAttempts)
                await MoveToDeadLetterQueueAsync(message, ex, deadLetterQueue, cancellationToken);
        }
    }

    private async Task MoveToDeadLetterQueueAsync(
        OutboxMessage message,
        Exception exception,
        IDeadLetterQueue deadLetterQueue,
        CancellationToken cancellationToken)
    {
        try
        {
            var kafkaMessage = CreateKafkaMessageFromOutbox(message);
            await deadLetterQueue.SendAsync(KafkaHeaderValues.OutboxSource, kafkaMessage, exception, cancellationToken);

            message.ProcessedAt = DateTime.UtcNow;
            message.ErrorMessage = $"MOVED TO DLQ: {exception.Message}";

            logger.LogWarning("Outbox message {MessageId} moved to DLQ after {Attempts} attempts. Error: {Error}",
                message.Id, message.ProcessingAttempts, exception.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to move outbox message {MessageId} to DLQ", message.Id);
        }
    }

    private Message<string, string> CreateKafkaMessageFromOutbox(OutboxMessage message)
    {
        return new Message<string, string>
        {
            Key = message.AggregateId,
            Value = message.Payload,
            Headers = new Headers
            {
                { KafkaHeaderKeys.EventType, Encoding.UTF8.GetBytes(message.EventType) },
                { KafkaHeaderKeys.AggregateId, Encoding.UTF8.GetBytes(message.AggregateId) },
                { KafkaHeaderKeys.AggregateType, Encoding.UTF8.GetBytes(message.AggregateType) },
                { KafkaHeaderKeys.OutboxMessageId, Encoding.UTF8.GetBytes(message.Id.ToString()) },
                { KafkaHeaderKeys.OriginalSource, KafkaHeaderValues.SourceOutboxPublisherBytes }
            }
        };
    }

    private string GetSimpleTypeName(string assemblyQualifiedName)
    {
        try
        {
            var parts = assemblyQualifiedName.Split(',');
            if (parts.Length > 0)
            {
                var fullTypeName = parts[0].Trim();
                var lastDotIndex = fullTypeName.LastIndexOf('.');
                if (lastDotIndex >= 0)
                    return fullTypeName.Substring(lastDotIndex + 1);
                return fullTypeName;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse type name: {AssemblyQualifiedName}", assemblyQualifiedName);
        }

        var cleaned = assemblyQualifiedName.Split(',')[0];
        var lastDot = cleaned.LastIndexOf('.');
        if (lastDot >= 0)
            return cleaned.Substring(lastDot + 1);
        return assemblyQualifiedName;
    }
}