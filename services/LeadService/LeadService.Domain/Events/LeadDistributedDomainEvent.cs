using AvroSchemas;
using AvroSchemas.Messages.LeadEvents;
using SharedKernel.Events;

namespace LeadService.Domain.Events;

/// <summary>
/// Доменное событие - успешное распределение лида.
/// </summary>
public sealed class LeadDistributedDomainEvent(Guid leadId, string target) : DomainEvent
{
    public Guid LeadId { get; } = leadId;
    public string Target { get; } = target;

    public override IIntegrationEvent ToIntegrationEvent()
    {
        return new LeadDistributed
        {
            EventId = EventId,
            OccurredOnUtc = new DateTimeOffset(OccurredOn).ToUnixTimeMilliseconds(),
            EventType = GetType().Name,
            SchemaVersion = 1,
            LeadId = LeadId,
            Target = Target
        };
    }
}