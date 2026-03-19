namespace IntegrationEvents.Base;

/// <summary>
/// Базовый класс для всех интеграционных событий
/// </summary>
public abstract class IntegrationEvent : IIntegrationEvent
{
    protected IntegrationEvent()
    {
        EventId = Guid.NewGuid();
        OccurredOnUtc = DateTime.UtcNow;
        SchemaVersion = 1;
        EventType = GetType().Name;
    }

    public Guid EventId { get; set; }

    public DateTime OccurredOnUtc { get; set; }

    public string? EventType { get; set; }

    public int SchemaVersion { get; set; }
}