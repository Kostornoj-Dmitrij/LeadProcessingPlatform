using System.Diagnostics.CodeAnalysis;
using Avro;
using Avro.Specific;
using MediatR;

namespace AvroSchemas.Messages.Base;

/// <summary>
/// Базовый класс для всех Avro-событий
/// </summary>
public abstract class IntegrationEventAvro : ISpecificRecord, IIntegrationEvent, INotification
{
    protected IntegrationEventAvro()
    {
        EventId = Guid.NewGuid();
        OccurredOnUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        SchemaVersion = 1;
        EventType = GetType().Name;
    }

    public abstract Schema Schema { get; }

    public Guid EventId { get; set; }
    public long OccurredOnUtc { get; set; }
    public string? EventType { get; set; }
    public int SchemaVersion { get; set; }

    DateTime IIntegrationEvent.OccurredOnUtc 
    { 
        get => DateTimeOffset.FromUnixTimeMilliseconds(OccurredOnUtc).UtcDateTime;
        set => OccurredOnUtc = new DateTimeOffset(value).ToUnixTimeMilliseconds();
    }

    [field: AllowNull, MaybeNull]
    public string AssemblyQualifiedName => field ??= GetType().AssemblyQualifiedName!;

    public abstract object? Get(int fieldPos);
    public abstract void Put(int fieldPos, object fieldValue);
}