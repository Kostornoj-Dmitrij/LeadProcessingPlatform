namespace ScoringService.Infrastructure.Inbox;

/// <summary>
/// Сообщение для паттерна Transactional Inbox
/// </summary>
public class InboxMessage
{
    public Guid Id { get; set; }

    public string MessageId { get; set; } = string.Empty;

    public string Topic { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public string? TraceId { get; set; }

    public DateTime ReceivedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public int ProcessingAttempts { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime? NextRetryAt { get; set; }
}