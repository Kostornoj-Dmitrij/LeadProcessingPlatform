using AvroSchemas;
using AvroSchemas.Messages.EnrichmentEvents;
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

    public override IIntegrationEvent ToIntegrationEvent()
    {
        return new LeadEnrichmentFailed
        {
            EventId = EventId,
            OccurredOnUtc = new DateTimeOffset(OccurredOn).ToUnixTimeMilliseconds(),
            EventType = GetType().Name,
            SchemaVersion = 1,
            LeadId = LeadId,
            Reason = Reason,
            RetryCount = RetryCount
        };
    }
}