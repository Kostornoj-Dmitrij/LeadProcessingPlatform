using AvroSchemas;
using AvroSchemas.Messages.EnrichmentEvents;
using SharedKernel.Events;

namespace EnrichmentService.Domain.Events;

/// <summary>
/// Доменное событие - выполнена компенсация обогащения лида
/// </summary>
public class LeadEnrichmentCompensatedDomainEvent(Guid leadId) : DomainEvent
{
    public Guid LeadId { get; } = leadId;
    public bool Compensated { get; } = true;

    public override IIntegrationEvent ToIntegrationEvent()
    {
        return new LeadEnrichmentCompensated
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