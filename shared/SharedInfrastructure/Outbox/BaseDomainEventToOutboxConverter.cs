using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SharedKernel.Entities;
using SharedKernel.Events;
using SharedKernel.Json;

namespace SharedInfrastructure.Outbox;

/// <summary>
/// Базовый конвертер доменных событий в outbox-сообщения
/// </summary>
public abstract class BaseDomainEventToOutboxConverter(ILogger logger) : IDomainEventToOutboxConverter
{
    public virtual List<OutboxMessage> Convert(
        string aggregateId, 
        string aggregateType, 
        IEnumerable<IDomainEvent> domainEvents)
    {
        var outboxMessages = new List<OutboxMessage>();

        var currentActivity = Activity.Current;
        logger.LogDebug("Converting domain events for aggregate {AggregateId}, TraceId: {TraceId}",
            aggregateId, currentActivity?.TraceId.ToString() ?? "none");

        foreach (var domainEvent in domainEvents)
        {
            var integrationEvent = domainEvent.ToIntegrationEvent();
            if (integrationEvent == null)
                continue;

            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                AggregateType = aggregateType,
                AggregateId = aggregateId,
                EventType = integrationEvent.GetType().AssemblyQualifiedName!,
                Payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), JsonDefaults.Options),
                CreatedAt = DateTime.UtcNow,
                ProcessingAttempts = 0,

                TraceParent = currentActivity != null 
                    ? $"00-{currentActivity.TraceId}-{currentActivity.SpanId}-01"
                    : null,
                TraceState = currentActivity?.TraceStateString
            };

            outboxMessages.Add(outboxMessage);

            logger.LogDebug(
                "Converted domain event {DomainEvent} to integration event {IntegrationEvent} for aggregate {AggregateId}, TraceId: {TraceId}",
                domainEvent.GetType().Name,
                integrationEvent.GetType().Name,
                aggregateId,
                currentActivity?.TraceId.ToString() ?? "none");
        }

        return outboxMessages;
    }
}