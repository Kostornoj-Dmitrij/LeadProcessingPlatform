namespace LeadService.Application.DTOs;

/// <summary>
/// DTO для возврата данных лида клиенту
/// </summary>
public class LeadDto
{
    public Guid Id { get; set; }
    public string? ExternalLeadId { get; set; }
    public string Source { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? Score { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Dictionary<string, string>? CustomFields { get; set; }
}