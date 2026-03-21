using SharedKernel.Entities;
using SharedKernel.Events;

namespace EnrichmentService.Infrastructure.Outbox;

/// <summary>
/// Конвертирует доменные события в outbox-сообщения
/// </summary>
public interface IDomainEventToOutboxConverter
{
    List<OutboxMessage> Convert(string aggregateId, string aggregateType, IEnumerable<IDomainEvent> domainEvents);
}