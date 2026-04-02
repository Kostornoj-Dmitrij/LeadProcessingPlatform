using System.Diagnostics;

namespace SharedInfrastructure.Telemetry;

/// <summary>
/// Методы расширения для работы с Activity
/// </summary>
public static class ActivityExtensions
{
    public static Activity? StartCommandHandlerSpan(this ActivitySource source, string commandName)
    {
        return source.StartSpan($"{TelemetrySpanNames.CommandHandler} {commandName}");
    }

    public static Activity? StartEventHandlerSpan(this ActivitySource source, string eventName)
    {
        return source.StartSpan($"{TelemetrySpanNames.EventHandler} {eventName}");
    }

    public static Activity? StartProducerSpan(this ActivitySource source, string operation, string eventName)
    {
        return source.StartActivity($"{operation} {eventName}", ActivityKind.Producer);
    }

    private static Activity? StartSpan(this ActivitySource source, string name)
    {
        return source.StartActivity(name);
    }

    public static Activity? AddTags(this Activity? activity, params (string key, object? value)[] tags)
    {
        if (activity == null) return null;

        foreach (var (key, value) in tags)
        {
            if (value != null)
            {
                activity.SetTag(key, value);
            }
        }
        return activity;
    }
}