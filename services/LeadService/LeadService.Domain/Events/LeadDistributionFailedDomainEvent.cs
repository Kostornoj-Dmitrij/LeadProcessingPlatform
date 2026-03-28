using AvroSchemas;
using AvroSchemas.Messages.LeadEvents;
using SharedKernel.Events;

namespace LeadService.Domain.Events;

/// <summary>
/// Доменное событие - ошибка распределения лида.
/// </summary>
public sealed class LeadDistributionFailedDomainEvent(Guid leadId, string reason) : DomainEvent
{
    public Guid LeadId { get; } = leadId;
    public string Reason { get; } = reason;

    public override IIntegrationEvent ToIntegrationEvent()
    {
        return new LeadDistributionFailed
        {
            EventId = EventId,
            OccurredOnUtc = new DateTimeOffset(OccurredOn).ToUnixTimeMilliseconds(),
            EventType = GetType().Name,
            SchemaVersion = 1,
            LeadId = LeadId,
            Reason = Reason
        };
    }
}