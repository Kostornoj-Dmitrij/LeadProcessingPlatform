using System.Diagnostics;

namespace SharedInfrastructure.Telemetry;

/// <summary>
/// Константы телеметрии
/// </summary>
public static class TelemetryConstants
{
    public static readonly ActivitySource ActivitySource = new("SharedInfrastructure");
}