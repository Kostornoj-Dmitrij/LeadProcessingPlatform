using AvroSchemas;
using AvroSchemas.Messages.LeadEvents;
using LeadService.Domain.Enums;
using SharedKernel.Events;

namespace LeadService.Domain.Events;

/// <summary>
/// Доменное событие - лид закрыт.
/// </summary>
public class LeadClosedDomainEvent(Guid leadId, LeadStatus previousStatus) : DomainEvent
{
    public Guid LeadId { get; } = leadId;
    public LeadStatus PreviousStatus { get; } = previousStatus;

    public override IIntegrationEvent ToIntegrationEvent()
    {
        return PreviousStatus switch
        {
            LeadStatus.Rejected => new LeadRejectedFinal
            {
                EventId = EventId,
                OccurredOnUtc = new DateTimeOffset(OccurredOn).ToUnixTimeMilliseconds(),
                EventType = GetType().Name,
                SchemaVersion = 1,
                LeadId = LeadId,
                FinalStatus = "Closed"
            },
            LeadStatus.FailedDistribution => new LeadDistributionFailedFinal
            {
                EventId = EventId,
                OccurredOnUtc = new DateTimeOffset(OccurredOn).ToUnixTimeMilliseconds(),
                EventType = GetType().Name,
                SchemaVersion = 1,
                LeadId = LeadId,
                FinalStatus = "Closed"
            },
            LeadStatus.Distributed => new LeadDistributedFinal
            {
                EventId = EventId,
                OccurredOnUtc = new DateTimeOffset(OccurredOn).ToUnixTimeMilliseconds(),
                EventType = GetType().Name,
                SchemaVersion = 1,
                LeadId = LeadId,
                FinalStatus = "Closed"
            },
            _ => null!
        };
    }
}