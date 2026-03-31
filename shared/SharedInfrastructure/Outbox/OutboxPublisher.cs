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
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);
    private readonly int _batchSize = 100;
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

                ActivityContext? parentContext = null;
                if (!string.IsNullOrEmpty(message.TraceParent))
                {
                    if (ActivityContext.TryParse(message.TraceParent, message.TraceState, out var parsedContext))
                    {
                        parentContext = parsedContext;
                        logger.LogDebug("Restored trace context from outbox: TraceId={TraceId}", 
                            parentContext.Value.TraceId);
                    }
                }

                using var activity = parentContext.HasValue 
                    ? TelemetryConstants.ActivitySource.StartActivity(
                        "OutboxPublish", 
                        ActivityKind.Internal, 
                        parentContext.Value)
                    : TelemetryConstants.ActivitySource.StartActivity("OutboxPublish");

                if (activity != null)
                {
                    activity.SetTag("outbox.message_id", message.Id);
                    activity.SetTag("outbox.event_type", message.EventType);
                    activity.SetTag("outbox.aggregate_id", message.AggregateId);
                }

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
}