using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SharedInfrastructure.Telemetry;
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

        var traceParent = TraceContextCarrier.TraceParent;
        if (string.IsNullOrEmpty(traceParent))
        {
            var currentActivity = Activity.Current;
            traceParent = currentActivity != null 
                ? $"00-{currentActivity.TraceId}-{currentActivity.SpanId}-01"
                : null;
        }

        logger.LogDebug("Converting domain events for aggregate {AggregateId}, TraceId: {TraceId}",
            aggregateId, traceParent ?? "none");

        foreach (var domainEvent in domainEvents)
        {
            var integrationEvent = domainEvent.ToIntegrationEvent();
            if (integrationEvent == null)
                continue;

            var eventType = integrationEvent.GetType();

            var assemblyQualifiedName = integrationEvent is AvroSchemas.Messages.Base.IntegrationEventAvro avroEvent
                ? avroEvent.AssemblyQualifiedName
                : eventType.AssemblyQualifiedName!;

            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                AggregateType = aggregateType,
                AggregateId = aggregateId,
                EventType = assemblyQualifiedName,
                Payload = JsonSerializer.Serialize(integrationEvent, eventType, JsonDefaults.Options),
                CreatedAt = DateTime.UtcNow,
                ProcessingAttempts = 0,
                TraceParent = traceParent,
                TraceState = Activity.Current?.TraceStateString
            };

            outboxMessages.Add(outboxMessage);

            logger.LogDebug(
                "Converted domain event {DomainEvent} to integration event {IntegrationEvent} for aggregate {AggregateId}, TraceId: {TraceId}",
                domainEvent.GetType().Name,
                eventType.Name,
                aggregateId,
                traceParent ?? "none");
        }

        return outboxMessages;
    }
}