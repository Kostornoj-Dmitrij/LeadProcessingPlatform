using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SharedHosting.Options;
using SharedHosting.Filters;

namespace SharedHosting.Extensions;

/// <summary>
/// Расширения для настройки OpenTelemetry
/// </summary>
public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddSharedOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        string[]? additionalSources = null)
    {
        var otelOptions = configuration.GetSection(OpenTelemetryOptions.SectionName)
            .Get<OpenTelemetryOptions>() ?? new OpenTelemetryOptions();

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName)
                .AddTelemetrySdk()
                .AddAttributes([
                    new KeyValuePair<string, object>("deployment.environment", 
                        configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development"),
                    new KeyValuePair<string, object>("service.version", 
                        typeof(OpenTelemetryExtensions).Assembly.GetName().Version?.ToString() ?? "1.0.0")
                ]));

        if (otelOptions.EnableTracing)
        {
            services.ConfigureOpenTelemetryTracerProvider(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.Filter = httpContext =>
                        {
                            if (otelOptions.FilterPaths != null)
                            {
                                foreach (var path in otelOptions.FilterPaths)
                                {
                                    if (httpContext.Request.Path.StartsWithSegments(path))
                                        return false;
                                }
                            }
                            return true;
                        };
                        options.RecordException = true;
                    })
                    .AddHttpClientInstrumentation()
                    .AddSource($"{serviceName}.InboxProcessor")
                    .AddSource($"{serviceName}.OutboxPublisher")
                    .AddSource($"{serviceName}.KafkaConsumer");

                if (additionalSources != null)
                {
                    foreach (var source in additionalSources)
                    {
                        tracing.AddSource(source);
                    }
                }

                if (otelOptions.FilterBackgroundQueries)
                {
                    tracing.AddProcessor<DatabaseFilterProcessor>();
                }

                tracing.AddOtlpExporter(options => 
                    options.Endpoint = new Uri(otelOptions.Endpoint));
            });
        }

        if (otelOptions.EnableMetrics)
        {
            services.ConfigureOpenTelemetryMeterProvider(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter(
                        "Microsoft.AspNetCore.Hosting",
                        "Microsoft.AspNetCore.Server.Kestrel",
                        "System.Net.Http",
                        $"{serviceName}.InboxProcessor",
                        $"{serviceName}.OutboxPublisher")
                    .AddOtlpExporter(options => 
                        options.Endpoint = new Uri(otelOptions.Endpoint));
            });
        }

        return services;
    }
}