using AvroSchemas;
using SharedKernel.Events;

namespace LeadService.Domain.Events;

/// <summary>
/// Доменное событие - получен результат скоринга лида
/// </summary>
public class ScoringReceivedDomainEvent(Guid leadId) : DomainEvent
{
    public Guid LeadId { get; } = leadId;

    public override IIntegrationEvent ToIntegrationEvent()
    {
        return null!;
    }
}