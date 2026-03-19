namespace SharedKernel.Entities;

/// <summary>
/// Сообщение для паттерна Transactional Outbox
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; }

    public string AggregateType { get; set; } = string.Empty;

    public string AggregateId { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public int ProcessingAttempts { get; set; }

    public string? ErrorMessage { get; set; }
}