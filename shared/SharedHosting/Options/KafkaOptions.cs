namespace SharedHosting.Options;

/// <summary>
/// Настройки подключения к Kafka
/// </summary>
public class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = string.Empty;

    public string SchemaRegistryUrl { get; set; } = string.Empty;

    public string GroupId { get; set; } = string.Empty;

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string SecurityProtocol { get; set; } = "SaslPlaintext";

    public string SaslMechanism { get; set; } = "ScramSha256";

    public int ConsumerMaxPollIntervalMs { get; set; } = 300000;

    public int ConsumerSessionTimeoutMs { get; set; } = 30000;

    public int ConsumerMaxPollRecords { get; set; } = 500;

    public int ConsumerFetchMinBytes { get; set; } = 1024;

    public int ConsumerFetchMaxWaitMs { get; set; } = 50;

    public bool ProducerEnableIdempotence { get; set; } = true;

    public int ProducerMaxRetries { get; set; } = 3;
}