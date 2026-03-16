namespace LeadService.Application.Common.Interfaces;

/// <summary>
/// Абстракция для публикации событий
/// </summary>
public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class;
}