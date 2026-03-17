namespace LeadService.Infrastructure.Inbox;

/// <summary>
/// Интерфейс для работы с Inbox хранилищем
/// </summary>
public interface IInboxStore
{
    Task<bool> TryAddAsync(
        string messageId, 
        string topic, 
        string key, 
        string eventType, 
        string payload,
        string? traceId,
        CancellationToken cancellationToken = default);

    Task<List<InboxMessage>> GetPendingMessagesAsync(
        int batchSize, 
        CancellationToken cancellationToken = default);

    Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);

    Task IncrementAttemptsAsync(
        Guid messageId, 
        string errorMessage,
        DateTime? nextRetryAt,
        CancellationToken cancellationToken = default);

    Task<InboxMessage?> GetByMessageIdAsync(
        string messageId, 
        CancellationToken cancellationToken = default);

    Task MoveToDeadLetterQueueAsync(Guid messageId, string errorMessage, CancellationToken cancellationToken = default);
}