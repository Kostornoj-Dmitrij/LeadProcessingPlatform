namespace AvroSchemas;

/// <summary>
/// Базовый интерфейс для всех интеграционных событий
/// </summary>
public interface IIntegrationEvent
{
    Guid EventId { get; set; }
    DateTime OccurredOnUtc { get; set; }
    string? EventType { get; set; }
    int SchemaVersion { get; set; }
}