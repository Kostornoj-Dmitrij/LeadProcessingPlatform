using System.Diagnostics.Metrics;

namespace SharedInfrastructure.Telemetry;

/// <summary>
/// Общие метрики для всех микросервисов
/// </summary>
public static class TelemetryMetrics
{
    private static readonly Meter Meter = new("SharedInfrastructure.Metrics", "1.0.0");

    public static readonly Counter<int> KafkaMessagesPublished = 
        Meter.CreateCounter<int>("kafka.messages.published", 
            description: "Number of messages published to Kafka by topic");
}