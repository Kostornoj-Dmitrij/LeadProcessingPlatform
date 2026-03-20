using System.Diagnostics;
using System.Text;

namespace ApiGateway.Host.Middleware;

/// <summary>
/// Middleware для логирования HTTP запросов и ответов через API Gateway
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

        string requestBody = string.Empty;
        if (context.Request.ContentLength > 0 && context.Request.ContentLength < 10240)
        {
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        logger.LogInformation(
            "API Gateway Request {TraceId} - {Method} {Path} - Query: {Query} - Body: {Body}",
            traceId,
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString,
            requestBody.Length > 500 ? requestBody[..500] + "..." : requestBody);
    }

    private async Task LogResponse(HttpContext context, MemoryStream responseBody, long elapsedMs)
    {
        responseBody.Seek(0, SeekOrigin.Begin);
        string responseContent = string.Empty;

        if (responseBody.Length > 0 && responseBody.Length < 10240)
        {
            using var reader = new StreamReader(responseBody, Encoding.UTF8, leaveOpen: true);
            responseContent = await reader.ReadToEndAsync();
        }

        responseBody.Seek(0, SeekOrigin.Begin);

        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        logger.LogInformation(
            "API Gateway Response {TraceId} - Status: {StatusCode} - Body: {Body} - Duration: {ElapsedMs}ms",
            traceId,
            context.Response.StatusCode,
            responseContent.Length > 500 ? responseContent[..500] + "..." : responseContent,
            elapsedMs);
    }
}