using System.ComponentModel.DataAnnotations;

namespace LeadService.Domain.Entities;

/// <summary>
/// История изменений статуса лида
/// </summary>
public class LeadStatusHistory
{
    public Guid Id { get; set; }

    public Guid LeadId { get; set; }

    public string? OldStatus { get; set; }

    public string NewStatus { get; set; } = string.Empty;

    public DateTime ChangedAt { get; set; }

    [MaxLength(1000)]
    public string? Reason { get; set; }

    public Guid? EventId { get; set; }

    public Lead? Lead { get; set; }
}