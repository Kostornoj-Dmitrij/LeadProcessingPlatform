using SharedKernel.Events;

namespace LeadService.Domain.Events;

/// <summary>
/// Доменное событие - успешное распределение лида.
/// </summary>
public sealed class LeadDistributedDomainEvent(Guid leadId, string target) : DomainEvent
{
    public Guid LeadId { get; } = leadId;
    public string Target { get; } = target;
}