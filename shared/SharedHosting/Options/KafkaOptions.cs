namespace SharedHosting.Options;

/// <summary>
/// Настройки подключения к Kafka
/// </summary>
public class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = string.Empty;

    public string GroupId { get; set; } = string.Empty;

    public string DlqTopic { get; set; } = string.Empty;

    public int ConsumerMaxPollIntervalMs { get; set; } = 300000;

    public int ConsumerSessionTimeoutMs { get; set; } = 30000;

    public bool ProducerEnableIdempotence { get; set; } = true;

    public int ProducerMaxRetries { get; set; } = 3;
}