using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;
using LeadService.Application.Common.Interfaces;
using LeadService.Infrastructure.Data;
using LeadService.Infrastructure.Inbox;
using IntegrationEvents;
using SharedKernel.Entities;
using Confluent.Kafka;

namespace LeadService.Infrastructure.Outbox;

/// <summary>
/// Фоновый сервис для публикации сообщений из Outbox в Kafka
/// </summary>
public class OutboxPublisher(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxPublisher> logger)
    : BackgroundService
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
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
        var deadLetterQueue = scope.ServiceProvider.GetRequiredService<IDeadLetterQueue>();

        var messages = await context.OutboxMessages
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

                var @event = JsonSerializer.Deserialize(message.Payload, eventType);
                if (@event == null)
                {
                    logger.LogWarning("Failed to deserialize event: {EventType}", message.EventType);
                    await MoveToDeadLetterQueueAsync(message,
                        new Exception($"Failed to deserialize event: {message.EventType}"),
                        deadLetterQueue, cancellationToken);
                    continue;
                }

                if (@event is not IIntegrationEvent integrationEvent)
                {
                    logger.LogWarning("Event {EventType} does not implement IIntegrationEvent", message.EventType);
                    await MoveToDeadLetterQueueAsync(message,
                        new Exception($"Event does not implement IIntegrationEvent: {message.EventType}"),
                        deadLetterQueue, cancellationToken);
                    continue;
                }

                await eventBus.PublishAsync(integrationEvent, cancellationToken);
                
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
            
            logger.LogWarning(
                "Outbox message {MessageId} moved to DLQ after {Attempts} attempts. Error: {Error}",
                message.Id,
                message.ProcessingAttempts,
                exception.Message);
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