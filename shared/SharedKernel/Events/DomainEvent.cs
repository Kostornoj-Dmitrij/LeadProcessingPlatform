using MediatR;

namespace SharedKernel.Events;

/// <summary>
/// Базовый класс для всех доменных событий
/// </summary>
public abstract class DomainEvent(Guid eventId, DateTime occurredOn) : IDomainEvent, INotification
{
    public Guid EventId { get; } = eventId;
    public DateTime OccurredOn { get; } = occurredOn;

    protected DomainEvent() : this(Guid.NewGuid(), DateTime.UtcNow)
    {
    }
}