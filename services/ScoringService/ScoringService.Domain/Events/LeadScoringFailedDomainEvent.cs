using SharedKernel.Events;

namespace ScoringService.Domain.Events;

/// <summary>
/// Доменное событие - ошибка при выполнении скоринга
/// </summary>
public class LeadScoringFailedDomainEvent(Guid leadId, string reason, int retryCount) : DomainEvent
{
    public Guid LeadId { get; } = leadId;
    public string Reason { get; } = reason;
    public int RetryCount { get; } = retryCount;
}