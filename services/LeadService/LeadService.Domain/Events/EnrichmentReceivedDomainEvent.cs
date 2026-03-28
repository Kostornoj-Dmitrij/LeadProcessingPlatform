using AvroSchemas;
using SharedKernel.Events;

namespace LeadService.Domain.Events;

/// <summary>
/// Доменное событие - получено обогащение лида.
/// </summary>
public class EnrichmentReceivedDomainEvent(Guid leadId) : DomainEvent
{
    public Guid LeadId { get; } = leadId;

    public override IIntegrationEvent ToIntegrationEvent()
    {
        return null!;
    }
}