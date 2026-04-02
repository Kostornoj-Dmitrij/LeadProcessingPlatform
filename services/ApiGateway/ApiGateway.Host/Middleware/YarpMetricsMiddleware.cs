using System.Diagnostics;
using ApiGateway.Host.Metrics;

namespace ApiGateway.Host.Middleware;

/// <summary>
/// Middleware для сбора метрик проксирования
/// </summary>
public class YarpMetricsMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        GatewayMetrics.GatewayRequests.Add(1, new TagList 
        { 
            { "path", context.Request.Path },
            { "method", context.Request.Method }
        });

        context.Response.OnStarting(() =>
        {
            var duration = stopwatch.Elapsed.TotalMilliseconds;
            
            GatewayMetrics.ProxyDuration.Record(duration, new TagList
            {
                { "path", context.Request.Path },
                { "method", context.Request.Method },
                { "status_code", context.Response.StatusCode }
            });

            return Task.CompletedTask;
        });

        await next(context);
    }
}