using System.Diagnostics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

namespace EnrichmentService.Host.Extensions;

/// <summary>
/// Настройка OpenTelemetry для сбора метрик, трассировки и логирования
/// </summary>
public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddOpenTelemetryConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
    {
        var otlpEndpoint = configuration["OpenTelemetry:Endpoint"] ?? "http://aspire-dashboard:18889";

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName)
                .AddTelemetrySdk()
                .AddAttributes([
                    new KeyValuePair<string, object>("deployment.environment", 
                        configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development")
                ]))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.Filter = httpContext =>
                        !httpContext.Request.Path.StartsWithSegments("/health") &&
                        !httpContext.Request.Path.StartsWithSegments("/swagger");
                    options.RecordException = true;
                })
                .AddHttpClientInstrumentation()
                .AddSource("EnrichmentService.InboxProcessor")
                .AddSource("EnrichmentService.OutboxPublisher")
                .AddSource("EnrichmentService.KafkaConsumer")
                .AddProcessor(new DatabaseFilterProcessor())
                .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint)))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter(
                    "Microsoft.AspNetCore.Hosting",
                    "Microsoft.AspNetCore.Server.Kestrel",
                    "System.Net.Http",
                    "EnrichmentService.InboxProcessor",
                    "EnrichmentService.OutboxPublisher")
                .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint)));

        return services;
    }
}

public class DatabaseFilterProcessor : OpenTelemetry.BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (IsBackgroundDatabaseQuery(activity))
        {
            activity.IsAllDataRequested = false;
        }
    }

    private bool IsBackgroundDatabaseQuery(Activity activity)
    {
        foreach (var tag in activity.Tags)
        {
            if (tag.Key == "db.statement")
            {
                var sql = tag.Value ?? "";
                if (sql.Contains("inbox_messages") || sql.Contains("outbox_messages"))
                {
                    return true;
                }
            }
        }

        if (activity.DisplayName.Contains("inbox") ||
            activity.DisplayName.Contains("outbox"))
        {
            return true;
        }

        return false;
    }
}