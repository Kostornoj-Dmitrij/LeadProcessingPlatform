using System.Diagnostics;
using System.Text.Json;

namespace ApiGateway.Host.Middleware;

/// <summary>
/// Глобальный обработчик исключений для API Gateway
/// </summary>
public class GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        logger.LogError(exception, "An unhandled exception occurred in API Gateway");

        var response = context.Response;
        response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            HttpRequestException { StatusCode: not null } httpEx => (
                (int)httpEx.StatusCode.Value,
                $"Upstream service error: {httpEx.Message}"
            ),
            TaskCanceledException or OperationCanceledException => (
                504,
                "Request timeout while waiting for upstream service"
            ),
            _ => (
                500,
                "An internal server error occurred in API Gateway"
            )
        };

        response.StatusCode = statusCode;

        var errorResponse = new
        {
            Status = statusCode,
            Message = message,
            TraceId = Activity.Current?.Id ?? context.TraceIdentifier,
            Path = context.Request.Path.ToString()
        };

        await response.WriteAsync(JsonSerializer.Serialize(errorResponse));
    }
}