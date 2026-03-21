using System.Diagnostics;
using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Json;

namespace EnrichmentService.Host.Middleware;

/// <summary>
/// Глобальный обработчик исключений
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
        logger.LogError(exception, "An unhandled exception occurred");

        var response = context.Response;
        response.ContentType = "application/json";

        var (statusCode, message, details) = exception switch
        {
            ValidationException validationEx => (
                400,
                "Validation failed",
                validationEx.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
            ),

            InvalidOperationException { Message: "Request is being processed by another instance" } => (
                409,
                "Request is being processed by another instance",
                null
            ),

            InvalidOperationException invalidOpEx => (
                400,
                invalidOpEx.Message,
                null
            ),

            ArgumentException argEx => (
                400,
                argEx.Message,
                null
            ),

            KeyNotFoundException keyNotFoundEx => (
                404,
                keyNotFoundEx.Message,
                null
            ),

            DbUpdateConcurrencyException => (
                409,
                "Concurrency conflict occurred. Please retry.",
                null
            ),

            Npgsql.NpgsqlException { IsTransient: true } => (
                503,
                "Database temporarily unavailable. Please retry.",
                null
            ),

            _ => (
                500,
                "An internal server error occurred.",
                null
            )
        };

        response.StatusCode = statusCode;

        var errorResponse = new
        {
            Status = statusCode,
            Message = message,
            Details = details,
            TraceId = Activity.Current?.Id ?? context.TraceIdentifier
        };

        await response.WriteAsync(JsonSerializer.Serialize(errorResponse, JsonDefaults.Options));
    }
}