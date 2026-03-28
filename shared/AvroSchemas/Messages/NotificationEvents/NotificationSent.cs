using Avro;
using AvroSchemas.Messages.Base;

// ReSharper disable InconsistentNaming

namespace AvroSchemas.Messages.NotificationEvents;

/// <summary>
/// Avro-событие, публикуемое после отправки уведомления о результате обработки лида
/// </summary>
public class NotificationSent : IntegrationEventAvro
{
    private static readonly Schema _schema = Schema.Parse(@"{
        ""type"": ""record"",
        ""name"": ""NotificationSent"",
        ""namespace"": ""AvroSchemas.Messages.NotificationEvents"",
        ""fields"": [
            { ""name"": ""EventId"", ""type"": { ""type"": ""string"", ""logicalType"": ""uuid"" } },
            { ""name"": ""OccurredOnUtc"", ""type"": ""long"" },
            { ""name"": ""EventType"", ""type"": [""null"", ""string""] },
            { ""name"": ""SchemaVersion"", ""type"": ""int"" },
            { ""name"": ""LeadId"", ""type"": { ""type"": ""string"", ""logicalType"": ""uuid"" } },
            { ""name"": ""NotificationType"", ""type"": ""string"" },
            { ""name"": ""Channel"", ""type"": ""string"" },
            { ""name"": ""Status"", ""type"": ""string"" }
        ]
    }");

    public override Schema Schema => _schema;

    public Guid LeadId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public override object? Get(int fieldPos)
    {
        return fieldPos switch
        {
            0 => EventId.ToString(),
            1 => OccurredOnUtc,
            2 => EventType,
            3 => SchemaVersion,
            4 => LeadId.ToString(),
            5 => NotificationType,
            6 => Channel,
            7 => Status,
            _ => throw new AvroRuntimeException($"Invalid field position: {fieldPos}")
        };
    }

    public override void Put(int fieldPos, object fieldValue)
    {
        switch (fieldPos)
        {
            case 0: EventId = Guid.Parse(fieldValue.ToString()!); break;
            case 1: OccurredOnUtc = Convert.ToInt64(fieldValue); break;
            case 2: EventType = fieldValue as string; break;
            case 3: SchemaVersion = Convert.ToInt32(fieldValue); break;
            case 4: LeadId = Guid.Parse(fieldValue.ToString()!); break;
            case 5: NotificationType = fieldValue.ToString() ?? string.Empty; break;
            case 6: Channel = fieldValue.ToString() ?? string.Empty; break;
            case 7: Status = fieldValue.ToString() ?? string.Empty; break;
            default: throw new AvroRuntimeException($"Invalid field position: {fieldPos}");
        }
    }
}