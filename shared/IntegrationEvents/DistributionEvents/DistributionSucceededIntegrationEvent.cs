using IntegrationEvents.Base;

namespace IntegrationEvents.DistributionEvents;

/// <summary>
/// Публикуется Distribution Service после успешного распределения лида.
/// </summary>
public class DistributionSucceededIntegrationEvent : IntegrationEvent
{
    public Guid LeadId { get; set; }

    public string Target { get; set; } = string.Empty;

    public DateTime DistributedAt { get; set; }
}