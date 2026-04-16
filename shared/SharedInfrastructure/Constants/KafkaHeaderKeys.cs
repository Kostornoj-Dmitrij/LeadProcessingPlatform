namespace SharedInfrastructure.Constants;

/// <summary>
/// Ключи заголовков Kafka сообщений
/// </summary>
public static class KafkaHeaderKeys
{
    public const string EventType = "event-type";
    public const string EventId = "event-id";
    public const string ContentType = "content-type";
    public const string Timestamp = "timestamp";
    public const string TraceParent = "traceparent";
    public const string TraceState = "tracestate";
    public const string LeadId = "lead-id";
    public const string BaggagePrefix = "baggage-";
    public const string OriginalTopic = "original-topic";
    public const string OriginalPartition = "original-partition";
    public const string OriginalOffset = "original-offset";
    public const string ErrorMessage = "error-message";
    public const string ErrorType = "error-type";
    public const string Source = "source";
    public const string ServiceName = "service-name";
    public const string AggregateId = "aggregate-id";
    public const string AggregateType = "aggregate-type";
    public const string OutboxMessageId = "outbox-message-id";
    public const string OriginalSource = "original-source";
    public const string InboxMessageId = "inbox-message-id";
    public const string MessageId = "message-id";
}