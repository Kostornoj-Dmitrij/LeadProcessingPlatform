using System.Diagnostics;

// ReSharper disable InconsistentNaming

namespace SharedInfrastructure.Telemetry;

/// <summary>
/// Хранилище контекста трассировки для сквозной передачи traceparent
/// </summary>
public static class TraceContextCarrier
{
    private static readonly AsyncLocal<string?> _traceParent = new();
    private static readonly AsyncLocal<Dictionary<string, string>?> _baggage = new();

    public static string? TraceParent
    {
        get => _traceParent.Value ?? Activity.Current?.Id;
        set => _traceParent.Value = value;
    }

    public static void SetBaggage(string key, string value)
    {
        _baggage.Value ??= new Dictionary<string, string>();
        _baggage.Value[key] = value;
    }

    public static IEnumerable<KeyValuePair<string, string>> GetBaggage()
        => _baggage.Value ?? new Dictionary<string, string>();

    public static void Clear()
    {
        _traceParent.Value = null;
        _baggage.Value = null;
    }
}