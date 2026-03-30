using System.Text;
using System.Text.Json;
using AvroSchemas;
using Confluent.Kafka;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(2);
    private readonly int _batchSize = 50;
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

        var @event = JsonSerializer.Deserialize(message.Payload, eventType, JsonDefaults.Options) as IIntegrationEvent;
        if (@event == null)
            throw new InvalidOperationException($"Failed to deserialize event: {message.EventType}");

        await mediator.Publish(@event, cancellationToken);
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