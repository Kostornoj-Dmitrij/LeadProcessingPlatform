namespace AvroSchemas.Messages.LeadEvents;

/// <summary>
/// DTO для обогащенных данных
/// </summary>
public class EnrichedDataDto
{
    public string Industry { get; set; } = string.Empty;

    public string CompanySize { get; set; } = string.Empty;

    public string? Website { get; set; }

    public string? RevenueRange { get; set; }

    public int Version { get; set; }
}