using AvroSchemas;
using AvroSchemas.Messages.DistributionEvents;
using SharedKernel.Events;

namespace DistributionService.Domain.Events;

/// <summary>
/// Доменное событие - ошибка распределения лида
/// </summary>
public class DistributionFailedDomainEvent(
    Guid leadId,
    string reason) : DomainEvent
{
    public Guid LeadId { get; } = leadId;

    public string Reason { get; } = reason;

    public override IIntegrationEvent ToIntegrationEvent()
    {
        return new DistributionFailed
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