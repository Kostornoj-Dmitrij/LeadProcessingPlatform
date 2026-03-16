namespace IntegrationEvents.LeadEvents;

public class EnrichedDataDto
{
    public string Industry { get; set; } = string.Empty;
    public string CompanySize { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string? RevenueRange { get; set; }
}