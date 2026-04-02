using System.Diagnostics.Metrics;

namespace ScoringService.Application.Metrics;

/// <summary>
/// Метрики для Scoring Service
/// </summary>
public static class ScoringMetrics
{
    private static readonly Meter Meter = new("ScoringService.Metrics", "1.0.0");

    public static readonly Counter<int> ScoringRequests = 
        Meter.CreateCounter<int>("scoring.requests.total", 
            description: "Total number of scoring requests by status");

    public static readonly Counter<int> ScoringSuccess = 
        Meter.CreateCounter<int>("scoring.success.total", 
            description: "Total number of successful scoring operations");

    public static readonly Counter<int> ScoringFailure = 
        Meter.CreateCounter<int>("scoring.failure.total", 
            description: "Total number of failed scoring operations by error type");

    public static readonly Counter<int> RulesEvaluated = 
        Meter.CreateCounter<int>("scoring.rules.evaluated", 
            description: "Number of scoring rules evaluated by rule name");
}