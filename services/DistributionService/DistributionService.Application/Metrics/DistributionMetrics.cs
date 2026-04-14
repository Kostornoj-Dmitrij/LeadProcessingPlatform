using System.Diagnostics.Metrics;

namespace DistributionService.Application.Metrics;

/// <summary>
/// Метрики для Distribution Service
/// </summary>
public static class DistributionMetrics
{
    private static readonly Meter Meter = new("DistributionService.Metrics", "1.0.0");

    public static readonly Counter<int> DistributionRequests = 
        Meter.CreateCounter<int>("distribution.requests.total", 
            description: "Total number of distribution requests by status");

    public static readonly Counter<int> DistributionSuccess = 
        Meter.CreateCounter<int>("distribution.success.total", 
            description: "Total number of successful distributions by target");

    public static readonly Counter<int> DistributionFailure = 
        Meter.CreateCounter<int>("distribution.failure.total", 
            description: "Total number of failed distributions by target and error type");

    public static readonly Counter<int> DistributionRetry = 
        Meter.CreateCounter<int>("distribution.retry.total", 
            description: "Total number of retry attempts by attempt number");

    public static readonly Histogram<double> DistributionDuration = 
        Meter.CreateHistogram<double>("distribution.duration", unit: "ms", 
            description: "Duration of distribution operation by target and success status");

    public static readonly Counter<int> DistributionHttpStatusCodes = 
        Meter.CreateCounter<int>("distribution.http.status_codes", 
            description: "HTTP status codes returned by distribution targets");
}