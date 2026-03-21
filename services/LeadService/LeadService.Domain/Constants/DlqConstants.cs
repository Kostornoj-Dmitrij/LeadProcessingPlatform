namespace LeadService.Domain.Constants;

/// <summary>
/// Константы для Dead Letter Queue
/// </summary>
public static class DlqConstants
{
    public const string ErrorMessagePrefix = "MOVED TO DLQ: ";

    public const string OutboxSource = "outbox";

    public const string KafkaConsumerSource = "kafka-consumer";

    public const string InboxProcessorSource = "inbox-processor";
}