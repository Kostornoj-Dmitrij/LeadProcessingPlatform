using System.Text.Json;
using IntegrationEvents;
using IntegrationEvents.ScoringEvents;
using ScoringService.Domain.Events;
using Microsoft.Extensions.Logging;
using SharedInfrastructure.Outbox;
using SharedKernel.Entities;
using SharedKernel.Events;
using SharedKernel.Json;

namespace ScoringService.Infrastructure.Outbox;

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
            if (integrationEvent == null) continue;

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
            LeadScoredDomainEvent e => new LeadScoredIntegrationEvent
            {
                LeadId = e.LeadId,
                TotalScore = e.TotalScore,
                QualifiedThreshold = e.QualifiedThreshold,
                AppliedRules = e.AppliedRules
            },
            LeadScoringFailedDomainEvent e => new LeadScoringFailedIntegrationEvent
            {
                LeadId = e.LeadId,
                Reason = e.Reason,
                RetryCount = e.RetryCount
            },
            LeadScoringCompensatedDomainEvent e => new LeadScoringCompensatedIntegrationEvent
            {
                LeadId = e.LeadId,
                Compensated = e.Compensated
            },
            _ => null
        };
    }
}