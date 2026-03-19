using System.Text.Json;
using IntegrationEvents;
using IntegrationEvents.LeadEvents;
using LeadService.Domain.Enums;
using SharedKernel.Entities;
using SharedKernel.Events;
using LeadService.Domain.Events;
using Microsoft.Extensions.Logging;
using SharedKernel.Json;
using EnrichedDataDto = IntegrationEvents.LeadEvents.EnrichedDataDto;

namespace LeadService.Infrastructure.Outbox;

/// <summary>
/// Реализация конвертера доменных событий в outbox-сообщения
/// </summary>
public class DomainEventToOutboxConverter(ILogger<DomainEventToOutboxConverter> logger) : IDomainEventToOutboxConverter
{
    public List<OutboxMessage> Convert(
        string aggregateId, 
        string aggregateType, 
        IEnumerable<IDomainEvent> domainEvents)
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
            LeadCreatedDomainEvent e => new LeadCreatedIntegrationEvent
            {
                LeadId = e.LeadId,
                ExternalLeadId = e.ExternalLeadId,
                Source = e.Source,
                CompanyName = e.CompanyName,
                ContactPerson = e.ContactPerson,
                Email = e.Email,
                Phone = e.Phone,
                CustomFields = e.CustomFields
            },

            LeadQualifiedDomainEvent e => new LeadQualifiedIntegrationEvent
            {
                LeadId = e.LeadId,
                CompanyName = e.CompanyName,
                ContactPerson = e.ContactPerson,
                Email = e.Email,
                Score = e.Score,
                EnrichedData = e.EnrichedData != null ? new EnrichedDataDto
                {
                    Industry = e.EnrichedData.Industry,
                    CompanySize = e.EnrichedData.CompanySize,
                    Website = e.EnrichedData.Website,
                    RevenueRange = e.EnrichedData.RevenueRange
                } : null
            },

            LeadRejectedDomainEvent e => new LeadRejectedIntegrationEvent
            {
                LeadId = e.LeadId,
                Reason = e.Reason,
                FailureType = e.FailureType,
                ErrorDetails = e.Reason
            },

            LeadDistributedDomainEvent e => new LeadDistributedIntegrationEvent
            {
                LeadId = e.LeadId,
                Target = e.Target
            },

            LeadDistributionFailedDomainEvent e => new LeadDistributionFailedIntegrationEvent
            {
                LeadId = e.LeadId,
                Reason = e.Reason
            },

            EnrichmentCompensatedDomainEvent => null,

            ScoringCompensatedDomainEvent => null,

            LeadClosedDomainEvent { PreviousStatus: LeadStatus.Rejected } e => 
                new LeadRejectedFinalIntegrationEvent
                {
                    LeadId = e.LeadId,
                    FinalStatus = "Closed"
                },

            LeadClosedDomainEvent { PreviousStatus: LeadStatus.FailedDistribution } e => 
                new LeadDistributionFailedFinalIntegrationEvent
                {
                    LeadId = e.LeadId,
                    FinalStatus = "Closed"
                },

            LeadClosedDomainEvent { PreviousStatus: LeadStatus.Distributed } e => 
                new LeadDistributedFinalIntegrationEvent
                {
                    LeadId = e.LeadId,
                    FinalStatus = "Closed"
                },

            _ => null
        };
    }
}