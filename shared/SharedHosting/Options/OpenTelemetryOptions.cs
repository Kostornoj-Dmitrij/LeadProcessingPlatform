namespace SharedHosting.Options;

/// <summary>
/// Настройки OpenTelemetry
/// </summary>
public class OpenTelemetryOptions
{
    public const string SectionName = "OpenTelemetry";

    public string Endpoint { get; set; } = "http://aspire-dashboard:18889";

    public bool EnableTracing { get; set; } = true;

    public bool EnableMetrics { get; set; } = true;

    public string[]? FilterPaths { get; set; } = ["/health", "/swagger", "/metrics"];

    public bool FilterBackgroundQueries { get; set; } = true;
}