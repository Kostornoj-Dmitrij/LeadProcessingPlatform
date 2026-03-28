using AvroSchemas;
using SharedKernel.Events;

namespace LeadService.Domain.Events;

/// <summary>
/// Доменное событие - выполнена компенсация скоринга
/// </summary>
public class ScoringCompensatedDomainEvent(Guid leadId) : DomainEvent
{
    public Guid LeadId { get; } = leadId;

    public override IIntegrationEvent ToIntegrationEvent()
    {
        return null!;
    }
}