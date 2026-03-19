using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

namespace LeadService.Host.Extensions;

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
                .AddSource("LeadService.InboxProcessor")
                .AddSource("LeadService.OutboxPublisher")
                .AddSource("LeadService.KafkaConsumer")
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
                    "LeadService.InboxProcessor",
                    "LeadService.OutboxPublisher")
                .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint)));

        return services;
    }
}