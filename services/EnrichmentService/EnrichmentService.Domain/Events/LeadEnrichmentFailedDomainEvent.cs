using SharedKernel.Events;

namespace EnrichmentService.Domain.Events;

/// <summary>
/// Доменное событие - ошибка при обогащении лида
/// </summary>
public class LeadEnrichmentFailedDomainEvent(Guid leadId, string reason, int retryCount) : DomainEvent
{
    public Guid LeadId { get; } = leadId;

    public string Reason { get; } = reason;

    public int RetryCount { get; } = retryCount;
}