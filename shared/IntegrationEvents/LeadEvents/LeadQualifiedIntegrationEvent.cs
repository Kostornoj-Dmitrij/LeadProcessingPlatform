using IntegrationEvents.Base;

namespace IntegrationEvents.LeadEvents;

/// <summary>
/// Публикуется Lead Service после успешного обогащения и скоринга.
/// </summary>
public class LeadQualifiedIntegrationEvent : IntegrationEvent
{
    public Guid LeadId { get; set; }

    public string CompanyName { get; set; } = string.Empty;

    public string? ContactPerson { get; set; }

    public string Email { get; set; } = string.Empty;

    public int Score { get; set; }

    public EnrichedDataDto? EnrichedData { get; set; }
}