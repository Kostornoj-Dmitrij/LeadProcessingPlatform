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
using SharedHosting.Telemetry;
using SharedInfrastructure.Constants;
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
    private readonly int _batchSize = 200;
    private const int MaxRetryAttempts = 5;

    private readonly TimeSpan _minInterval = TimeSpan.FromMilliseconds(10);
    private readonly TimeSpan _maxInterval = TimeSpan.FromMilliseconds(500);
    private TimeSpan _currentInterval = TimeSpan.FromMilliseconds(100);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Inbox Processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            bool hasWork;
            try
            {
                hasWork = await ProcessPendingMessages(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing inbox messages");
                hasWork = true;
            }

            _currentInterval = hasWork 
                ? _minInterval 
                : TimeSpan.FromTicks(Math.Min(_currentInterval.Ticks * 2, _maxInterval.Ticks));

            await Task.Delay(_currentInterval, stoppingToken);
        }

        logger.LogInformation("Inbox Processor stopped");
    }

    private async Task<bool> ProcessPendingMessages(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var inboxStore = scope.ServiceProvider.GetRequiredService<TInboxStore>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var deadLetterQueue = scope.ServiceProvider.GetRequiredService<IDeadLetterQueue>();

        var messages = await inboxStore.GetPendingMessagesAsync(_batchSize, cancellationToken);

        if (!messages.Any())
            return false;

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

        return true;
    }

    private async Task ProcessMessageAsync(
        InboxMessage message,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var eventType = EventTypeRegistry.GetType(message.EventType);
        if (eventType == null)
            throw new InvalidOperationException($"Unknown event type: {message.EventType}");

        string leadId = ExtractLeadIdFromPayload(message.Payload);
        string eventTypeShort = GetSimpleTypeName(message.EventType);

        if (!string.IsNullOrEmpty(message.TraceId))
        {
            var newSpanId = ActivitySpanId.CreateRandom().ToString();
            var traceParent = $"00-{message.TraceId}-{newSpanId}-01";
            TraceContextCarrier.TraceParent = traceParent;
        }

        var @event = JsonSerializer.Deserialize(message.Payload, eventType, JsonDefaults.Options) as IIntegrationEvent;
        if (@event == null)
            throw new InvalidOperationException($"Failed to deserialize event: {message.EventType}");

        if (TelemetryConstants.ActivitySource != null)
        {
            var traceParent = TraceContextCarrier.TraceParent;
            using var activity = ActivityBuilder.RestoreAndCreateActivity(
                    $"{TelemetrySpanNames.InboxProcess} {eventTypeShort}",
                    traceParent)
                .WithInboxProcessorTags(
                    message.EventType,
                    eventTypeShort,
                    leadId,
                    message.Topic,
                    message.Id,
                    message.ProcessingAttempts);

            await mediator.Publish(@event, cancellationToken);
        }
        else
        {
            await mediator.Publish(@event, cancellationToken);
        }
    }

    private Message<string, string> CreateKafkaMessageFromInbox(InboxMessage message)
    {
        return new Message<string, string>
        {
            Key = message.Key,
            Value = message.Payload,
            Headers = new Headers
            {
                { KafkaHeaderKeys.EventType, Encoding.UTF8.GetBytes(message.EventType) },
                { KafkaHeaderKeys.MessageId, Encoding.UTF8.GetBytes(message.MessageId) },
                { KafkaHeaderKeys.OriginalTopic, Encoding.UTF8.GetBytes(message.Topic) },
                { KafkaHeaderKeys.InboxMessageId, Encoding.UTF8.GetBytes(message.Id.ToString()) }
            }
        };
    }

    private string ExtractLeadIdFromPayload(string payload)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty(JsonPropertyKeys.LeadId, out var leadIdElement))
            {
                var leadId = leadIdElement.GetString();
                if (!string.IsNullOrEmpty(leadId))
                    return leadId;
            }

            if (doc.RootElement.TryGetProperty(JsonPropertyKeys.LeadIdLower, out var leadIdLowerElement))
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