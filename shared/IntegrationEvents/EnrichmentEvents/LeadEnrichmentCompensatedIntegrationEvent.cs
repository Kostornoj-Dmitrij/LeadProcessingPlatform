using IntegrationEvents.Base;

namespace IntegrationEvents.EnrichmentEvents;

/// <summary>
/// Публикуется Enrichment Service после успешной компенсации обогащения.
/// </summary>
public class LeadEnrichmentCompensatedIntegrationEvent : IntegrationEvent
{
    public Guid LeadId { get; set; }

    public bool Compensated { get; set; } = true;
}