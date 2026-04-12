namespace LoadTests.Host.Infrastructure;

/// <summary>
/// Информация о времени обработки одного лида
/// </summary>
public class LeadProcessingTime
{
    public Guid LeadId { get; set; }

    public string ExpectedPath { get; set; } = string.Empty;

    public string ActualPath { get; set; } = string.Empty;

    public double TotalSeconds { get; set; }
}