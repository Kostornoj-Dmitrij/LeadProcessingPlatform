using System.Text.Json.Serialization;

namespace LoadTests.Host.Infrastructure;

/// <summary>
/// Ответ от эндпоинта /api/leads/status-summary
/// </summary>
internal class StatusSummaryResponse
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("closed")]
    public int Closed { get; set; }

    [JsonPropertyName("rejected")]
    public int Rejected { get; set; }

    [JsonPropertyName("failedDistribution")]
    public int FailedDistribution { get; set; }

    [JsonPropertyName("inProgress")]
    public int InProgress { get; set; }

    [JsonPropertyName("allCompleted")]
    public bool AllCompleted { get; set; }
}