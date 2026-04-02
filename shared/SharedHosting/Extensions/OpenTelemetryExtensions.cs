using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Logs;
using OpenTelemetry.Exporter;
using SharedHosting.Filters;
using SharedHosting.Options;

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
        var otelOptions = configuration.GetSection("OpenTelemetry")
            .Get<OpenTelemetryOptions>() ?? new OpenTelemetryOptions();

        var activitySource = new System.Diagnostics.ActivitySource(serviceName);
        services.AddSingleton(activitySource);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: serviceName,
                    serviceVersion: "1.0.0")
                .AddTelemetrySdk()
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development"
                }));

        if (otelOptions.EnableTracing)
        {
            services.AddOpenTelemetry().WithTracing(tracing =>
            {
                tracing.AddSource(serviceName);
                tracing.AddSource("SharedInfrastructure");
                tracing.AddSource("Microsoft.AspNetCore");
                tracing.AddSource("Microsoft.EntityFrameworkCore");
                tracing.AddSource("Npgsql");
                tracing.AddSource("Confluent.Kafka");

                if (additionalSources != null)
                {
                    foreach (var source in additionalSources)
                    {
                        tracing.AddSource(source);
                    }
                }

                tracing
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = (httpContext) => !httpContext.Request.Path.StartsWithSegments("/health") &&
                                                          !httpContext.Request.Path.StartsWithSegments("/swagger");
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.FilterHttpRequestMessage = (httpRequestMessage) =>
                        {
                            var uri = httpRequestMessage.RequestUri?.AbsolutePath ?? "";
                            return !uri.Contains("/health") && 
                                   !uri.Contains("/subjects") &&
                                   !uri.Contains("/schemas"); 
                        };
                    })
                    .AddEntityFrameworkCoreInstrumentation(options =>
                    {
                        options.SetDbStatementForText = true;
                        options.SetDbStatementForStoredProcedure = true;
                        options.Filter = (_, command) =>
                        {
                            var commandText = command.CommandText.ToLowerInvariant();
                            return !commandText.Contains("inbox_messages") && 
                                   !commandText.Contains("outbox_messages") &&
                                   !commandText.Contains("pending_enriched_data") &&
                                   !commandText.Contains("scoring_requests") &&
                                   !commandText.Contains("scoring_rules") &&
                                   !commandText.Contains("enrichment_requests");
                        };
                    })
                    .AddNpgsql();

                tracing.AddProcessor(new DatabaseFilterProcessor());

                if (!string.IsNullOrEmpty(otelOptions.Endpoint))
                {
                    var useGrpc = otelOptions.Endpoint.Contains("4317") || 
                                   otelOptions.Protocol.Equals("Grpc", StringComparison.OrdinalIgnoreCase);

                    tracing.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otelOptions.Endpoint);
                        options.Protocol = useGrpc 
                            ? OtlpExportProtocol.Grpc 
                            : OtlpExportProtocol.HttpProtobuf;
                    });
                }
                else
                {
                    tracing.AddConsoleExporter();
                }
            });
        }

        if (otelOptions.EnableMetrics)
        {
            services.AddOpenTelemetry().WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(serviceName)
                    .AddMeter("LeadService.Metrics")
                    .AddMeter("EnrichmentService.Metrics")
                    .AddMeter("ScoringService.Metrics")
                    .AddMeter("DistributionService.Metrics")
                    .AddMeter("NotificationService.Metrics")
                    .AddMeter("ApiGateway.Metrics")
                    .AddMeter("SharedInfrastructure.Metrics")
                    .AddMeter("Microsoft.AspNetCore.Hosting")
                    .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
                    .AddMeter("System.Net.Http")
                    .AddMeter("Npgsql");

                if (additionalSources != null)
                {
                    foreach (var source in additionalSources)
                    {
                        metrics.AddMeter(source);
                    }
                }

                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (!string.IsNullOrEmpty(otelOptions.Endpoint))
                {
                    var useGrpc = otelOptions.Endpoint.Contains("4317") || 
                                   otelOptions.Protocol.Equals("Grpc", StringComparison.OrdinalIgnoreCase);

                    metrics.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otelOptions.Endpoint);
                        options.Protocol = useGrpc 
                            ? OtlpExportProtocol.Grpc 
                            : OtlpExportProtocol.HttpProtobuf;
                    });
                }
                else
                {
                    metrics.AddConsoleExporter();
                }
            });
        }

        services.AddLogging(logging =>
        {
            logging.AddOpenTelemetry(options =>
            {
                if (!string.IsNullOrEmpty(otelOptions.Endpoint))
                {
                    var useGrpc = otelOptions.Endpoint.Contains("4317") || 
                                   otelOptions.Protocol.Equals("Grpc", StringComparison.OrdinalIgnoreCase);

                    options.AddOtlpExporter(otlpOptions =>
                    {
                        otlpOptions.Endpoint = new Uri(otelOptions.Endpoint);
                        otlpOptions.Protocol = useGrpc 
                            ? OtlpExportProtocol.Grpc 
                            : OtlpExportProtocol.HttpProtobuf;
                    });
                }

                options.IncludeFormattedMessage = true;
                options.IncludeScopes = true;
            });
        });

        return services;
    }
}