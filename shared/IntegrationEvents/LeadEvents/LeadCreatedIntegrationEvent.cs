using IntegrationEvents.Base;

namespace IntegrationEvents.LeadEvents;

/// <summary>
/// Публикуется Lead Service при создании нового лида.
/// </summary>
public class LeadCreatedIntegrationEvent : IntegrationEvent
{
    public Guid LeadId { get; set; }

    public string? ExternalLeadId { get; set; }

    public string Source { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    public string? ContactPerson { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public Dictionary<string, string>? CustomFields { get; set; }
}