namespace SharedInfrastructure.Telemetry;

/// <summary>
/// Имена спанов для различных операций
/// </summary>
public static class TelemetrySpanNames
{
    public const string KafkaConsume = "Kafka.Consume";
    public const string KafkaProduce = "Kafka.Produce";
    public const string InboxProcess = "Inbox.Process";
    public const string OutboxPublish = "Outbox.Publish";
    public const string CommandHandler = "Command.Handler";
    public const string EventHandler = "Event.Handler";
    public const string DistributionClient = "Distribution.Client.Send";
    public const string EnrichmentProcess = "Enrichment.Process";
    public const string ScoringProcess = "Scoring.Process";
    public const string DistributionProcess = "Distribution.Process";
}