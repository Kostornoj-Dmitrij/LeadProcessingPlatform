using AvroSchemas;
using AvroSchemas.Messages.ScoringEvents;
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

    public override IIntegrationEvent ToIntegrationEvent()
    {
        return new LeadScoringFailed
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