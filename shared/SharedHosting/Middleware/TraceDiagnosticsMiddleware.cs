using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SharedHosting.Middleware;

/// <summary>
/// Middleware для диагностики трассировок
/// </summary>
public class TraceDiagnosticsMiddleware(RequestDelegate next, ILogger<TraceDiagnosticsMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? "none";
        var spanId = Activity.Current?.SpanId.ToString() ?? "none";

        logger.LogDebug("HTTP Request TraceId: {TraceId}, SpanId: {SpanId}, Path: {Path}",
            traceId, spanId, context.Request.Path);

        await next(context);
    }
}