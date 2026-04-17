namespace SharedHosting.Options;

/// <summary>
/// Настройки OpenTelemetry
/// </summary>
public class OpenTelemetryOptions
{
    public const string SectionName = "OpenTelemetry";

    public string Endpoint { get; set; } = "http://localhost:4317";

    public string Protocol { get; set; } = "Grpc";

    public bool EnableTracing { get; set; } = true;

    public bool EnableMetrics { get; set; } = true;

    public bool FilterBackgroundQueries { get; set; } = true;

    public List<string> DisabledMetrics { get; set; } = [];
}