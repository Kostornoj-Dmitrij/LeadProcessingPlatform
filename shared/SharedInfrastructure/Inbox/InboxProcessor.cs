using System.Diagnostics;
using System.Text;
using System.Text.Json;
using AvroSchemas;
using Confluent.Kafka;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharedInfrastructure.Telemetry;
using SharedKernel.Json;

namespace SharedInfrastructure.Inbox;

/// <summary>
/// Фоновый процессор для обработки сообщений из Inbox
/// </summary>
public class InboxProcessor<TInboxStore>(
    IServiceScopeFactory scopeFactory,
    ILogger<InboxProcessor<TInboxStore>> logger)
    : BackgroundService
    where TInboxStore : IInboxStore
{
    private readonly TimeSpan _pollingInterval = TimeSpan.FromMilliseconds(100);
    private readonly int _batchSize = 200;
    private const int MaxRetryAttempts = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Inbox Processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessages(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing inbox messages");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        logger.LogInformation("Inbox Processor stopped");
    }

    private async Task ProcessPendingMessages(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var inboxStore = scope.ServiceProvider.GetRequiredService<TInboxStore>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var deadLetterQueue = scope.ServiceProvider.GetRequiredService<IDeadLetterQueue>();

        var messages = await inboxStore.GetPendingMessagesAsync(_batchSize, cancellationToken);

        if (!messages.Any())
            return;

        logger.LogInformation("Processing {Count} inbox messages", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                await ProcessMessageAsync(message, mediator, cancellationToken);
                await inboxStore.MarkAsProcessedAsync(message.Id, cancellationToken);
                logger.LogDebug("Successfully processed inbox message {MessageId}", message.MessageId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process inbox message {MessageId}, attempt {Attempts}",
                    message.MessageId, message.ProcessingAttempts + 1);

                var shouldRetry = IsTransientError(ex) && message.ProcessingAttempts < MaxRetryAttempts;

                if (shouldRetry)
                {
                    var nextRetryAt = DateTime.UtcNow.AddSeconds(Math.Pow(2, message.ProcessingAttempts + 1));
                    await inboxStore.IncrementAttemptsAsync(message.Id, ex.Message, nextRetryAt, cancellationToken);
                    logger.LogInformation("Scheduled retry #{Attempts} for message {MessageId} at {NextRetryAt}",
                        message.ProcessingAttempts + 1, message.MessageId, nextRetryAt);
                }
                else
                {
                    var kafkaMessage = CreateKafkaMessageFromInbox(message);
                    await deadLetterQueue.SendAsync(message.Topic, kafkaMessage, ex, cancellationToken);
                    await inboxStore.MoveToDeadLetterQueueAsync(message.Id, ex.Message, cancellationToken);
                    logger.LogWarning("Message {MessageId} moved to DLQ after {Attempts} attempts",
                        message.MessageId, message.ProcessingAttempts + 1);
                }
            }
        }
    }

    private async Task ProcessMessageAsync(
        InboxMessage message,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var eventType = Type.GetType(message.EventType);
        if (eventType == null)
            throw new InvalidOperationException($"Unknown event type: {message.EventType}");

        string leadId = ExtractLeadIdFromPayload(message.Payload);
        string eventTypeShort = GetSimpleTypeName(message.EventType);

        ActivityContext? parentContext = TryRestoreContextFromTraceId(message.TraceId);
        using var activity = parentContext.HasValue
            ? TelemetryConstants.ActivitySource.StartActivity(
                $"{TelemetrySpanNames.InboxProcess} {eventTypeShort}",
                ActivityKind.Internal,
                parentContext.Value)
            : TelemetryConstants.ActivitySource.StartActivity(
                $"{TelemetrySpanNames.InboxProcess} {eventTypeShort}");

        if (activity == null)
            throw new InvalidOperationException("Failed to create activity");

        activity.AddTags(
            (TelemetryAttributes.EventType, message.EventType),
            (TelemetryAttributes.EventName, eventTypeShort),
            (TelemetryAttributes.LeadId, leadId),
            (TelemetryAttributes.KafkaTopic, message.Topic),
            (TelemetryAttributes.ProcessingStep, "inbox_process"),
            (TelemetryAttributes.InboxMessageId, message.Id),
            (TelemetryAttributes.InboxProcessingAttempts, message.ProcessingAttempts));

        var @event = JsonSerializer.Deserialize(message.Payload, eventType, JsonDefaults.Options) as IIntegrationEvent;
        if (@event == null)
            throw new InvalidOperationException($"Failed to deserialize event: {message.EventType}");

        await mediator.Publish(@event, cancellationToken);
    }

    private ActivityContext? TryRestoreContextFromTraceId(string? traceId)
    {
        if (string.IsNullOrEmpty(traceId))
            return null;

        try
        {
            var newSpanId = ActivitySpanId.CreateRandom().ToString();
            var traceparent = $"00-{traceId}-{newSpanId}-01";

            if (ActivityContext.TryParse(traceparent, null, out var parsedContext))
            {
                logger.LogDebug(
                    "Restored trace context from TraceId: TraceId={TraceId}, SpanId={SpanId}", 
                    parsedContext.TraceId, parsedContext.SpanId);
                return parsedContext;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to restore trace context from TraceId: {TraceId}", traceId);
        }

        return null;
    }

    private Message<string, string> CreateKafkaMessageFromInbox(InboxMessage message)
    {
        return new Message<string, string>
        {
            Key = message.Key,
            Value = message.Payload,
            Headers = new Headers
            {
                { "event-type", Encoding.UTF8.GetBytes(message.EventType) },
                { "message-id", Encoding.UTF8.GetBytes(message.MessageId) },
                { "original-topic", Encoding.UTF8.GetBytes(message.Topic) },
                { "inbox-message-id", Encoding.UTF8.GetBytes(message.Id.ToString()) }
            }
        };
    }

    private string ExtractLeadIdFromPayload(string payload)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty("LeadId", out var leadIdElement))
            {
                var leadId = leadIdElement.GetString();
                if (!string.IsNullOrEmpty(leadId))
                    return leadId;
            }

            if (doc.RootElement.TryGetProperty("leadId", out var leadIdLowerElement))
            {
                var leadId = leadIdLowerElement.GetString();
                if (!string.IsNullOrEmpty(leadId))
                    return leadId;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to extract LeadId from payload");
        }
        return string.Empty;
    }

    private string GetSimpleTypeName(string eventType)
    {
        try
        {
            var parts = eventType.Split(',');
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
            logger.LogWarning(ex, "Failed to parse event type: {EventType}", eventType);
        }

        var cleaned = eventType.Split(',')[0];
        var lastDot = cleaned.LastIndexOf('.');
        if (lastDot >= 0)
        {
            return cleaned.Substring(lastDot + 1);
        }

        return eventType;
    }

    private bool IsTransientError(Exception ex)
    {
        return ex switch
        {
            DbUpdateConcurrencyException => true,
            DbUpdateException dbEx when dbEx.InnerException?.Message.Contains("deadlock") == true => true,
            TimeoutException => true,
            Npgsql.NpgsqlException { IsTransient: true } => true,
            _ => false
        };
    }
}