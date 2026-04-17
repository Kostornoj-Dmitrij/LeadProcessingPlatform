using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Logs;
using OpenTelemetry.Exporter;
using SharedHosting.Constants;
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
        var otelOptions = configuration.GetSection(ConfigurationKeys.OpenTelemetrySection)
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
                    ["deployment.environment"] = configuration[ConfigurationKeys.AspNetCoreEnvironment] ?? "Development"
                }));

        if (otelOptions.EnableTracing)
        {
            services.AddOpenTelemetry().WithTracing(tracing =>
            {
                tracing.AddSource(serviceName);
                tracing.AddSource(TelemetrySourceNames.SharedInfrastructure);
                tracing.AddSource(TelemetrySourceNames.MicrosoftAspNetCore);
                tracing.AddSource(TelemetrySourceNames.MicrosoftEntityFrameworkCore);
                tracing.AddSource(TelemetrySourceNames.Npgsql);
                tracing.AddSource(TelemetrySourceNames.ConfluentKafka);

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
                        options.Filter = (httpContext) => !httpContext.Request.Path.StartsWithSegments(ConfigurationKeys.HealthPath) &&
                                                          !httpContext.Request.Path.StartsWithSegments(ConfigurationKeys.SwaggerPath);
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.FilterHttpRequestMessage = (httpRequestMessage) =>
                        {
                            var uri = httpRequestMessage.RequestUri?.AbsolutePath ?? "";
                            return !uri.Contains(ConfigurationKeys.HealthPath) && 
                                   !uri.Contains(ConfigurationKeys.SubjectsPath) &&
                                   !uri.Contains(ConfigurationKeys.SchemasPath);
                        };
                    })
                    .AddEntityFrameworkCoreInstrumentation(options =>
                    {
                        options.SetDbStatementForText = true;
                        options.SetDbStatementForStoredProcedure = true;
                        options.Filter = (_, command) =>
                        {
                            var commandText = command.CommandText.ToLowerInvariant();
                            return !commandText.Contains(BackgroundQueryPatterns.InboxMessages) && 
                                   !commandText.Contains(BackgroundQueryPatterns.OutboxMessages) &&
                                   !commandText.Contains(BackgroundQueryPatterns.PendingEnrichedData) &&
                                   !commandText.Contains(BackgroundQueryPatterns.ScoringRequests) &&
                                   !commandText.Contains(BackgroundQueryPatterns.ScoringRules) &&
                                   !commandText.Contains(BackgroundQueryPatterns.EnrichmentRequests) &&
                                   !commandText.Contains(BackgroundQueryPatterns.DistributionRequests);
                        };
                    })
                    .AddNpgsql();

                tracing.AddProcessor(new DatabaseFilterProcessor(configuration));

                if (!string.IsNullOrEmpty(otelOptions.Endpoint))
                {
                    var useGrpc = otelOptions.Endpoint.Contains(ConfigurationKeys.OtlpGrpcPort) || 
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
                var allMeters = new[]
                {
                    serviceName,
                    TelemetrySourceNames.LeadServiceMetrics,
                    TelemetrySourceNames.EnrichmentServiceMetrics,
                    TelemetrySourceNames.ScoringServiceMetrics,
                    TelemetrySourceNames.DistributionServiceMetrics,
                    TelemetrySourceNames.NotificationServiceMetrics,
                    TelemetrySourceNames.ApiGatewayMetrics,
                    TelemetrySourceNames.SharedInfrastructureMetrics,
                    TelemetrySourceNames.AspNetCoreHosting,
                    TelemetrySourceNames.AspNetCoreKestrel,
                    TelemetrySourceNames.SystemNetHttp,
                    TelemetrySourceNames.Npgsql};

                foreach (var meterName in allMeters)
                {
                    if (IsMeterEnabled(otelOptions, meterName))
                        metrics.AddMeter(meterName);
                }

                if (additionalSources != null)
                {
                    foreach (var source in additionalSources)
                    {
                        if (IsMeterEnabled(otelOptions, source))
                            metrics.AddMeter(source);
                    }
                }

                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (!string.IsNullOrEmpty(otelOptions.Endpoint))
                {
                    var useGrpc = otelOptions.Endpoint.Contains(ConfigurationKeys.OtlpGrpcPort) || 
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
                    var useGrpc = otelOptions.Endpoint.Contains(ConfigurationKeys.OtlpGrpcPort) || 
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

    private static bool IsMeterEnabled(OpenTelemetryOptions options, string meterName)
    {
        if (!options.EnableMetrics) return false;
        var disabled = options.DisabledMetrics;

        return !disabled.Contains(meterName);
    }
}