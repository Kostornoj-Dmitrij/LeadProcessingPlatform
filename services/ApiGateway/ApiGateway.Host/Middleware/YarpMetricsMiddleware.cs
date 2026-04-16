using System.Diagnostics;
using ApiGateway.Host.Metrics;

namespace ApiGateway.Host.Middleware;

/// <summary>
/// Middleware для сбора метрик проксирования
/// </summary>
public class YarpMetricsMiddleware(RequestDelegate next)
{
    private const string TagPath = "path";
    private const string TagMethod = "method";
    private const string TagStatusCode = "status_code";

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        GatewayMetrics.GatewayRequests.Add(1, new TagList 
        { 
            { TagPath, context.Request.Path },
            { TagMethod, context.Request.Method }
        });

        context.Response.OnStarting(() =>
        {
            var duration = stopwatch.Elapsed.TotalMilliseconds;
            
            GatewayMetrics.ProxyDuration.Record(duration, new TagList
            {
                { TagPath, context.Request.Path },
                { TagMethod, context.Request.Method },
                { TagStatusCode, context.Response.StatusCode }
            });

            return Task.CompletedTask;
        });

        await next(context);
    }
}