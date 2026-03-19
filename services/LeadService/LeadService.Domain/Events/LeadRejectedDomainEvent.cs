using SharedKernel.Events;

namespace LeadService.Domain.Events;

/// <summary>
/// Доменное событие - лид отклонен
/// </summary>
public class LeadRejectedDomainEvent(Guid leadId, string reason, string failureType) : DomainEvent
{
    public Guid LeadId { get; } = leadId;

    public string Reason { get; } = reason;

    public string FailureType { get; } = failureType;

}