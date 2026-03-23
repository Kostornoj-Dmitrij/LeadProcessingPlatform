namespace ScoringService.Host.Options;

/// <summary>
/// Настройки подключения к Kafka
/// </summary>
public class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = string.Empty;

    public string GroupId { get; set; } = "scoring-service";

    public string DlqTopic { get; set; } = "scoring-service-dlq";

    public int ConsumerMaxPollIntervalMs { get; set; } = 300000;

    public int ConsumerSessionTimeoutMs { get; set; } = 30000;

    public bool ProducerEnableIdempotence { get; set; } = true;

    public int ProducerMaxRetries { get; set; } = 3;
}