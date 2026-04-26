namespace LoadTests.Host.Infrastructure;

/// <summary>
/// DTO для чтения полей лида при валидации консистентности
/// </summary>
public class LeadConsistencyDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? Score { get; set; }
    public bool IsEnrichmentReceived { get; set; }
    public bool IsScoringReceived { get; set; }
    public bool IsEnrichmentCompensated { get; set; }
    public bool IsScoringCompensated { get; set; }
    public string? EnrichedData { get; set; }
}