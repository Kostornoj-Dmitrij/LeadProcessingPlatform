using AvroSchemas;
using AvroSchemas.Messages.ScoringEvents;
using SharedKernel.Events;

namespace ScoringService.Domain.Events;

/// <summary>
/// Доменное событие - выполнена компенсация скоринга
/// </summary>
public class LeadScoringCompensatedDomainEvent(Guid leadId) : DomainEvent
{
    public Guid LeadId { get; } = leadId;
    public bool Compensated { get; } = true;

    public override IIntegrationEvent ToIntegrationEvent()
    {
        return new LeadScoringCompensated
        {
            EventId = EventId,
            OccurredOnUtc = new DateTimeOffset(OccurredOn).ToUnixTimeMilliseconds(),
            EventType = GetType().Name,
            SchemaVersion = 1,
            LeadId = LeadId,
            Compensated = Compensated
        };
    }
}