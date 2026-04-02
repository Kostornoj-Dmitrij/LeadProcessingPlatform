using Avro;
using AvroSchemas.Messages.Base;

// ReSharper disable InconsistentNaming

namespace AvroSchemas.Messages.DistributionEvents;

/// <summary>
/// Avro-событие, публикуемое при ошибке распределения лида
/// </summary>
public class DistributionFailed : IntegrationEventAvro
{
    private static readonly Schema _schema = Schema.Parse(@"{
        ""type"": ""record"",
        ""name"": ""DistributionFailed"",
        ""namespace"": ""AvroSchemas.Messages.DistributionEvents"",
        ""fields"": [
            { ""name"": ""EventId"", ""type"": { ""type"": ""string"", ""logicalType"": ""uuid"" } },
            { ""name"": ""OccurredOnUtc"", ""type"": ""long"" },
            { ""name"": ""EventType"", ""type"": [""null"", ""string""] },
            { ""name"": ""SchemaVersion"", ""type"": ""int"" },
            { ""name"": ""LeadId"", ""type"": { ""type"": ""string"", ""logicalType"": ""uuid"" } },
            { ""name"": ""Reason"", ""type"": ""string"" },
            { ""name"": ""HttpStatusCode"", ""type"": [""null"", ""int""] }
        ]
    }");

    public override Schema Schema => _schema;

    public Guid LeadId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int? HttpStatusCode { get; set; }

    public override object? Get(int fieldPos)
    {
        return fieldPos switch
        {
            0 => EventId.ToString(),
            1 => OccurredOnUtc,
            2 => EventType,
            3 => SchemaVersion,
            4 => LeadId.ToString(),
            5 => Reason,
            6 => HttpStatusCode,
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
            case 5: Reason = fieldValue.ToString() ?? string.Empty; break;
            case 6: HttpStatusCode = fieldValue as int?; break;
            default: throw new AvroRuntimeException($"Invalid field position: {fieldPos}");
        }
    }
}