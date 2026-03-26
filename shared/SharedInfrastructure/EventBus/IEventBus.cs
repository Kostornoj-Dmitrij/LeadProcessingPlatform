namespace SharedInfrastructure.EventBus;

/// <summary>
/// Абстракция для публикации событий в шину сообщений (Kafka)
/// </summary>
public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class;
}