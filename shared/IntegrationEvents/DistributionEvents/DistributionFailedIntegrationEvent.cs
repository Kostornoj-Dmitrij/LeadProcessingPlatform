using IntegrationEvents.Base;

namespace IntegrationEvents.DistributionEvents;

/// <summary>
/// Публикуется Distribution Service при ошибке распределения.
/// </summary>
public class DistributionFailedIntegrationEvent : IntegrationEvent 
{
    public Guid LeadId { get; set; }

    public string Reason { get; set; } = string.Empty;

    public int? HttpStatusCode { get; set; }
}