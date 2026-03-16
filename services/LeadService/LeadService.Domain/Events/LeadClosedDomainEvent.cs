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
}