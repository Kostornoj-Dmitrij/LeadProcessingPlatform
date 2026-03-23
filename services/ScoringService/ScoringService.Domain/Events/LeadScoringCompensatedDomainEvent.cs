using SharedKernel.Events;

namespace ScoringService.Domain.Events;

/// <summary>
/// Доменное событие - выполнена компенсация скоринга
/// </summary>
public class LeadScoringCompensatedDomainEvent(Guid leadId) : DomainEvent
{
    public Guid LeadId { get; } = leadId;
    public bool Compensated { get; } = true;
}