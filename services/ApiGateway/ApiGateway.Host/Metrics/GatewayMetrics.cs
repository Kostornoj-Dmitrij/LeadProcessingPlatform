using System.Diagnostics.Metrics;

namespace ApiGateway.Host.Metrics;

/// <summary>
/// Метрики для API Gateway
/// </summary>
public static class GatewayMetrics
{
    public static readonly Meter Meter = new("ApiGateway.Metrics", "1.0.0");

    public static readonly Histogram<double> ProxyDuration = 
        Meter.CreateHistogram<double>("gateway.proxy.duration", unit: "ms", description: "Duration of proxy requests to backend");

    public static readonly Counter<int> GatewayRequests = 
        Meter.CreateCounter<int>("gateway.requests.total", description: "Total requests processed by gateway");
}