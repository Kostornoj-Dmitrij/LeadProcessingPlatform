using IntegrationEvents.Base;

namespace IntegrationEvents.LeadEvents;

/// <summary>
/// Публикуется Lead Service при закрытии лида после успешного распределения.
/// </summary>
public class LeadDistributedFinalIntegrationEvent : IntegrationEvent
{
    public Guid LeadId { get; set; }

    public string FinalStatus { get; set; } = "Closed";
}