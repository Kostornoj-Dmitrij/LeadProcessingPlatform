using MediatR;
using AvroSchemas;

namespace SharedKernel.Events;

/// <summary>
/// Базовый класс для всех доменных событий
/// </summary>
public abstract class DomainEvent(Guid eventId, DateTime occurredOn) : IDomainEvent, INotification
{
    protected DomainEvent() : this(Guid.NewGuid(), DateTime.UtcNow)
    {
    }

    public Guid EventId { get; } = eventId;
    public DateTime OccurredOn { get; } = occurredOn;

    public abstract IIntegrationEvent? ToIntegrationEvent();
}