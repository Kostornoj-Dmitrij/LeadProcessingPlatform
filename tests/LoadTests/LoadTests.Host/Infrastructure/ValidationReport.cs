namespace LoadTests.Host.Infrastructure;

/// <summary>
/// Отчёт о валидации консистентности обработки лидов
/// </summary>
public class ValidationReport
{
    public int TotalLeads { get; set; }
    public int LeadsWithHistory { get; set; }
    public int CompletedLeads { get; set; }
    public List<Guid> StuckLeads { get; set; } = [];
    public List<string> Errors { get; set; } = [];
    public List<LeadProcessingTime> ProcessingTimes { get; set; } = [];

    public bool IsValid => Errors.Count == 0 && StuckLeads.Count == 0 && TotalLeads == CompletedLeads;

    public void AddError(Guid leadId, string message)
    {
        Errors.Add($"Lead {leadId}: {message}");
    }

    public void AddError(string message)
    {
        Errors.Add(message);
    }
}