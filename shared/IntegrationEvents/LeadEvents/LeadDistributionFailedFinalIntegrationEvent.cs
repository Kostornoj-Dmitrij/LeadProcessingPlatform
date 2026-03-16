using IntegrationEvents.Base;

namespace IntegrationEvents.LeadEvents;

/// <summary>
/// Публикуется Lead Service после получения всех компенсаций при DistributionFailed.
/// </summary>
public class LeadDistributionFailedFinalIntegrationEvent : IntegrationEvent
{
    public Guid LeadId { get; set; }

    public string FinalStatus { get; set; } = "Closed";
}