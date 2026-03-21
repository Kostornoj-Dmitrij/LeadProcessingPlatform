using System.Text.Json;
using IntegrationEvents;
using IntegrationEvents.EnrichmentEvents;
using EnrichmentService.Domain.Events;
using Microsoft.Extensions.Logging;
using SharedKernel.Entities;
using SharedKernel.Events;
using SharedKernel.Json;

namespace EnrichmentService.Infrastructure.Outbox;

/// <summary>
/// Реализация конвертера доменных событий в outbox-сообщения
/// </summary>
public class DomainEventToOutboxConverter(ILogger<DomainEventToOutboxConverter> logger) : IDomainEventToOutboxConverter
{
    public List<OutboxMessage> Convert(string aggregateId, string aggregateType, IEnumerable<IDomainEvent> domainEvents)
    {
        var outboxMessages = new List<OutboxMessage>();

        foreach (var domainEvent in domainEvents)
        {
            var integrationEvent = MapToIntegrationEvent(domainEvent);
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
                ProcessingAttempts = 0
            };

            outboxMessages.Add(outboxMessage);

            logger.LogDebug(
                "Converted domain event {DomainEvent} to integration event {IntegrationEvent} for aggregate {AggregateId}",
                domainEvent.GetType().Name,
                integrationEvent.GetType().Name,
                aggregateId);
        }

        return outboxMessages;
    }

    private IIntegrationEvent? MapToIntegrationEvent(IDomainEvent domainEvent)
    {
        return domainEvent switch
        {
            LeadEnrichedDomainEvent e => new LeadEnrichedIntegrationEvent
            {
                LeadId = e.LeadId,
                Industry = e.Industry,
                CompanySize = e.CompanySize,
                Website = e.Website,
                RevenueRange = e.RevenueRange,
                Version = e.Version
            },
            LeadEnrichmentFailedDomainEvent e => new LeadEnrichmentFailedIntegrationEvent
            {
                LeadId = e.LeadId,
                Reason = e.Reason,
                RetryCount = e.RetryCount
            },
            LeadEnrichmentCompensatedDomainEvent e => new LeadEnrichmentCompensatedIntegrationEvent
            {
                LeadId = e.LeadId,
                Compensated = e.Compensated
            },
            _ => null
        };
    }
}