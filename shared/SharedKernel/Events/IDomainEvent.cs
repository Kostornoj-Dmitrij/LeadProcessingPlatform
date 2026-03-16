namespace SharedKernel.Events;

/// <summary>
/// Интерфейс для доменных событий.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}