using SharedKernel.Events;

namespace EnrichmentService.Domain.Events;

/// <summary>
/// Доменное событие - выполнена компенсация обогащения лида
/// </summary>
public class LeadEnrichmentCompensatedDomainEvent(Guid leadId) : DomainEvent
{
    public Guid LeadId { get; } = leadId;

    public bool Compensated { get; } = true;
}