namespace SharedHosting.Constants;

/// <summary>
/// Названия источников телеметрии
/// </summary>
public static class TelemetrySourceNames
{
    public const string SharedInfrastructure = "SharedInfrastructure";
    public const string MicrosoftAspNetCore = "Microsoft.AspNetCore";
    public const string MicrosoftEntityFrameworkCore = "Microsoft.EntityFrameworkCore";
    public const string Npgsql = "Npgsql";
    public const string ConfluentKafka = "Confluent.Kafka";

    public const string LeadServiceMetrics = "LeadService.Metrics";
    public const string EnrichmentServiceMetrics = "EnrichmentService.Metrics";
    public const string ScoringServiceMetrics = "ScoringService.Metrics";
    public const string DistributionServiceMetrics = "DistributionService.Metrics";
    public const string NotificationServiceMetrics = "NotificationService.Metrics";
    public const string ApiGatewayMetrics = "ApiGateway.Metrics";
    public const string SharedInfrastructureMetrics = "SharedInfrastructure.Metrics";
    public const string AspNetCoreHosting = "Microsoft.AspNetCore.Hosting";
    public const string AspNetCoreKestrel = "Microsoft.AspNetCore.Server.Kestrel";
    public const string SystemNetHttp = "System.Net.Http";
}