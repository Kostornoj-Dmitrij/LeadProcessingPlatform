using IntegrationEvents.Base;

namespace IntegrationEvents.EnrichmentEvents;

/// <summary>
/// Публикуется Enrichment Service после успешного обогащения лида.
/// </summary>
public class LeadEnrichedIntegrationEvent : IntegrationEvent
{
    public Guid LeadId { get; set; }

    public string Industry { get; set; } = string.Empty;

    public string CompanySize { get; set; } = string.Empty;

    public string? Website { get; set; }

    public string? RevenueRange { get; set; }

    public int Version { get; set; }
}