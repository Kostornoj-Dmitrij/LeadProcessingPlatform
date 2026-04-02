using System.Diagnostics;

namespace SharedInfrastructure.Telemetry;

/// <summary>
/// Класс для управления контекстом телеметрии
/// </summary>
public static class TelemetryContext
{
    public static void SetBaggage(string key, string value)
    {
        Activity.Current?.SetBaggage(key, value);
    }

    public static void SetLeadBaggage(Guid leadId, string businessProcess)
    {
        SetBaggage(TelemetryBaggageKeys.LeadId, leadId.ToString());
        SetBaggage(TelemetryBaggageKeys.BusinessProcess, businessProcess);
    }

    public static string? GetTraceParent()
    {
        var current = Activity.Current;
        return current != null ? $"00-{current.TraceId}-{current.SpanId}-01" : null;
    }
}