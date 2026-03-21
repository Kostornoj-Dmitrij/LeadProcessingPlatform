using Confluent.Kafka;

namespace EnrichmentService.Infrastructure.Inbox;

/// <summary>
/// Интерфейс для отправки сообщений в Dead Letter Queue
/// </summary>
public interface IDeadLetterQueue
{
    Task SendAsync(string source, Message<string, string> message, Exception exception, CancellationToken cancellationToken = default);
}