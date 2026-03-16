using IntegrationEvents.Base;

namespace IntegrationEvents.EnrichmentEvents;

/// <summary>
/// Публикуется Enrichment Service при ошибке обогащения.
/// </summary>
public class LeadEnrichmentFailedIntegrationEvent : IntegrationEvent
{
    public Guid LeadId { get; set; }

    public string Reason { get; set; } = string.Empty;

    public int RetryCount { get; set; }
}