using System.Diagnostics;

namespace SharedInfrastructure.Telemetry;

/// <summary>
/// Класс для восстановления контекста трассировки из TraceParent
/// </summary>
public static class TelemetryRestorer
{
    public static Activity? RestoreAndStartActivity(
        ActivitySource source,
        string operationName,
        string? traceParent,
        ActivityKind kind = ActivityKind.Internal)
    {
        if (!string.IsNullOrEmpty(traceParent) && ActivityContext.TryParse(traceParent, null, out var context))
        {
            return source.StartActivity(operationName, kind, context);
        }

        return source.StartActivity(operationName, kind);
    }
}