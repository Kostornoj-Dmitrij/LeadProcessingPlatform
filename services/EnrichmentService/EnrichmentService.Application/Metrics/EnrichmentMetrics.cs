using System.Diagnostics.Metrics;

namespace EnrichmentService.Application.Metrics;

/// <summary>
/// Метрики для Enrichment Service
/// </summary>
public static class EnrichmentMetrics
{
    private static readonly Meter Meter = new("EnrichmentService.Metrics", "1.0.0");

    public static readonly Counter<int> EnrichmentRequests = 
        Meter.CreateCounter<int>("enrichment.requests.total", 
            description: "Total number of enrichment requests by status");

    public static readonly Counter<int> EnrichmentSuccess = 
        Meter.CreateCounter<int>("enrichment.success.total", 
            description: "Total number of successful enrichments");

    public static readonly Counter<int> EnrichmentFailure = 
        Meter.CreateCounter<int>("enrichment.failure.total", 
            description: "Total number of failed enrichments by error type");

    public static readonly Counter<int> EnrichmentRetry = 
        Meter.CreateCounter<int>("enrichment.retry.total", 
            description: "Total number of retry attempts by attempt number");

    public static readonly Histogram<double> EnrichmentDuration = 
        Meter.CreateHistogram<double>("enrichment.duration", unit: "ms", 
            description: "Duration of enrichment processing");
}