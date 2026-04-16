using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
    private readonly TimeSpan _pollingInterval = TimeSpan.FromMilliseconds(100);
    private readonly int _batchSize = 200;
    private const int MaxRetryAttempts = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox Publisher started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishPendingMessages(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error publishing outbox messages");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        logger.LogInformation("Outbox Publisher stopped");
    }

    private async Task PublishPendingMessages(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
        var deadLetterQueue = scope.ServiceProvider.GetRequiredService<IDeadLetterQueue>();

        var messages = await context.Set<OutboxMessage>()
            .Where(x => x.ProcessedAt == null && x.ProcessingAttempts < MaxRetryAttempts)
            .OrderBy(x => x.CreatedAt)
            .Take(_batchSize)
            .ToListAsync(cancellationToken);

        if (!messages.Any())
            return;

        logger.LogInformation("Found {Count} pending outbox messages", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                var eventType = Type.GetType(message.EventType);
                if (eventType == null)
                {
                    logger.LogWarning("Unknown event type: {EventType}", message.EventType);
                    await MoveToDeadLetterQueueAsync(message,
                        new Exception($"Unknown event type: {message.EventType}"),
                        deadLetterQueue, cancellationToken);
                    continue;
                }

                var @event = JsonSerializer.Deserialize(message.Payload, eventType, JsonDefaults.Options);
                if (@event == null)
                {
                    logger.LogWarning("Failed to deserialize event: {EventType}", message.EventType);
                    await MoveToDeadLetterQueueAsync(message,
                        new Exception($"Failed to deserialize event: {message.EventType}"),
                        deadLetterQueue, cancellationToken);
                    continue;
                }

                string eventTypeShort = GetSimpleTypeName(message.EventType);

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

                var method = typeof(IEventBus).GetMethod("PublishAsync");
                if (method == null)
                    throw new InvalidOperationException("PublishAsync method not found");

                var genericMethod = method.MakeGenericMethod(eventType);
                var task = (Task)genericMethod.Invoke(eventBus, [@event, cancellationToken])!;
                await task;

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
                {
                    await MoveToDeadLetterQueueAsync(message, ex, deadLetterQueue, cancellationToken);
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);
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
            await deadLetterQueue.SendAsync("outbox", kafkaMessage, exception, cancellationToken);

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
                { "event-type", Encoding.UTF8.GetBytes(message.EventType) },
                { "aggregate-id", Encoding.UTF8.GetBytes(message.AggregateId) },
                { "aggregate-type", Encoding.UTF8.GetBytes(message.AggregateType) },
                { "outbox-message-id", Encoding.UTF8.GetBytes(message.Id.ToString()) },
                { "original-source", "outbox-publisher"u8.ToArray() }
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
                {
                    return fullTypeName.Substring(lastDotIndex + 1);
                }
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
        {
            return cleaned.Substring(lastDot + 1);
        }

        return assemblyQualifiedName;
    }
}