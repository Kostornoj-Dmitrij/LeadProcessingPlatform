using IntegrationEvents.Base;

namespace IntegrationEvents.LeadEvents;

/// <summary>
/// Публикуется Lead Service при ошибке распределения.
/// </summary>
public class LeadDistributionFailedIntegrationEvent : IntegrationEvent
{
    public Guid LeadId { get; set; }

    public string Reason { get; set; } = string.Empty;
}