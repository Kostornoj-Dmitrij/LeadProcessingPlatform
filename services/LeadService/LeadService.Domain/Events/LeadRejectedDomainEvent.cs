using AvroSchemas;
using AvroSchemas.Messages.LeadEvents;
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

    public override IIntegrationEvent ToIntegrationEvent()
    {
        return new LeadRejected
        {
            EventId = EventId,
            OccurredOnUtc = new DateTimeOffset(OccurredOn).ToUnixTimeMilliseconds(),
            EventType = GetType().Name,
            SchemaVersion = 1,
            LeadId = LeadId,
            Reason = Reason,
            FailureType = FailureType
        };
    }
}