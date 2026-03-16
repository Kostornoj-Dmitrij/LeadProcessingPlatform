using MediatR;
using IntegrationEvents;

namespace SharedKernel.Events;

/// <summary>
/// Обертка для передачи интеграционных событий через MediatR
/// </summary>
public class IntegrationEventWrapper<TEvent>(TEvent @event) : INotification
    where TEvent : class, IIntegrationEvent
{
    public TEvent Event { get; } = @event ?? throw new ArgumentNullException(nameof(@event));
}