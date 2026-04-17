using System.Diagnostics;

namespace SharedHosting.Telemetry;

/// <summary>
/// ActivitySource для создания спанов телеметрии
/// </summary>
public static class TelemetryConstants
{
    public static ActivitySource? ActivitySource { get; private set; }

    public static void Initialize(bool tracingEnabled)
    {
        ActivitySource = tracingEnabled ? new ActivitySource("SharedInfrastructure") : null;
    }
}