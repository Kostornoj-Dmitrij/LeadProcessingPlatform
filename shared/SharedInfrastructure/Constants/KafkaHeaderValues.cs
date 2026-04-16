namespace SharedInfrastructure.Constants;

/// <summary>
/// Значения заголовков Kafka сообщений
/// </summary>
public static class KafkaHeaderValues
{
    public const string OutboxSource = "outbox";
    public static readonly byte[] ContentTypeAvroBytes = "application/avro"u8.ToArray();
    public static readonly byte[] SourceKafkaConsumerBytes = "kafka-consumer"u8.ToArray();
    public static readonly byte[] SourceInboxProcessorBytes = "inbox-processor"u8.ToArray();
    public static readonly byte[] SourceOutboxPublisherBytes = "outbox-publisher"u8.ToArray();
}