using System.Diagnostics;
using System.Text;

namespace EnrichmentService.Host.Middleware;

/// <summary>
/// Middleware для логирования HTTP запросов и ответов
/// </summary>
public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        await LogRequest(context);

        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await next(context);

            await LogResponse(context, responseBody, stopwatch.ElapsedMilliseconds);

            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private async Task LogRequest(HttpContext context)
    {
        context.Request.EnableBuffering();

        var requestBody = await ReadStreamAsync(context.Request.Body);
        context.Request.Body.Seek(0, SeekOrigin.Begin);

        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        logger.LogInformation(
            "HTTP Request {TraceId} - {Method} {Path} - Query: {Query} - Body: {Body}",
            traceId,
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString,
            requestBody);
    }

    private async Task LogResponse(HttpContext context, MemoryStream responseBody, long elapsedMs)
    {
        responseBody.Seek(0, SeekOrigin.Begin);
        var responseContent = await ReadStreamAsync(responseBody);
        responseBody.Seek(0, SeekOrigin.Begin);

        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        logger.LogInformation(
            "HTTP Response {TraceId} - Status: {StatusCode} - Body: {Body} - Duration: {ElapsedMs}ms",
            traceId,
            context.Response.StatusCode,
            responseContent,
            elapsedMs);
    }

    private static async Task<string> ReadStreamAsync(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }
}