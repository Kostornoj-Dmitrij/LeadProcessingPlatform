namespace LeadService.Application.Common.DTOs;

/// <summary>
/// DTO для возврата сводки по статусам списка лидов
/// </summary>
public class LeadsStatusSummaryDto
{
    public int Total { get; set; }

    public int Closed { get; set; }

    public int Rejected { get; set; }

    public int FailedDistribution { get; set; }

    public int InProgress { get; set; }

    public bool AllCompleted => InProgress == 0;
}