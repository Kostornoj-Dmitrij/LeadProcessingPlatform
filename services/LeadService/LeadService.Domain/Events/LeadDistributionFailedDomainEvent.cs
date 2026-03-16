using SharedKernel.Events;

namespace LeadService.Domain.Events;

/// <summary>
/// Доменное событие - ошибка распределения лида.
/// </summary>
public sealed class LeadDistributionFailedDomainEvent(Guid leadId, string reason) : DomainEvent
{
    public Guid LeadId { get; } = leadId;
    public string Reason { get; } = reason;
}