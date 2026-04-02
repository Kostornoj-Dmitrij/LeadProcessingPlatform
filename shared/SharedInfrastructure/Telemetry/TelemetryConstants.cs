using System.Diagnostics;

namespace SharedInfrastructure.Telemetry;

/// <summary>
/// ActivitySource для создания спанов телеметрии
/// </summary>
public static class TelemetryConstants
{
    public static readonly ActivitySource ActivitySource = new("SharedInfrastructure");
}