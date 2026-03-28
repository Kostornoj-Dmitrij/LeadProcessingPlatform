using AvroSchemas;
using SharedKernel.Events;

namespace LeadService.Domain.Events;

/// <summary>
/// Доменное событие - выполнена компенсация обогащения.
/// </summary>
public class EnrichmentCompensatedDomainEvent(Guid leadId) : DomainEvent
{
    public Guid LeadId { get; } = leadId;

    public override IIntegrationEvent ToIntegrationEvent()
    {
        return null!;
    }
}