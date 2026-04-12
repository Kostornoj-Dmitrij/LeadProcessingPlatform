namespace LoadTests.Host.Infrastructure;

/// <summary>
/// Упрощённый DTO для чтения записей из таблицы lead_status_history (LeadService).
/// </summary>
public class LeadStatusHistoryDto
{
    public Guid LeadId { get; set; }

    public string? OldStatus { get; set; }

    public string NewStatus { get; set; } = string.Empty;

    public DateTime ChangedAt { get; set; }
}