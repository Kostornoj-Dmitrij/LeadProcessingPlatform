using AvroSchemas;
using AvroSchemas.Messages.DistributionEvents;
using SharedKernel.Events;

namespace DistributionService.Domain.Events;

/// <summary>
/// Доменное событие - лид успешно распределен
/// </summary>
public class DistributionSucceededDomainEvent(
    Guid leadId,
    string target,
    DateTime distributedAt) : DomainEvent
{
    public Guid LeadId { get; } = leadId;

    public string Target { get; } = target;

    public DateTime DistributedAt { get; } = distributedAt;

    public override IIntegrationEvent ToIntegrationEvent()
    {
        return new DistributionSucceeded
        {
            EventId = EventId,
            OccurredOnUtc = new DateTimeOffset(OccurredOn).ToUnixTimeMilliseconds(),
            EventType = GetType().Name,
            SchemaVersion = 1,
            LeadId = LeadId,
            Target = Target,
            DistributedAt = new DateTimeOffset(DistributedAt).ToUnixTimeMilliseconds()
        };
    }
}