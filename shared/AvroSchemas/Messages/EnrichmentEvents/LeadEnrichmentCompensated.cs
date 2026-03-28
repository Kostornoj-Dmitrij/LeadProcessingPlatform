using Avro;
using AvroSchemas.Messages.Base;

// ReSharper disable InconsistentNaming

namespace AvroSchemas.Messages.EnrichmentEvents;

/// <summary>
/// Avro-событие, публикуемое после выполнения компенсации обогащения лида
/// </summary>
public class LeadEnrichmentCompensated : IntegrationEventAvro
{
    private static readonly Schema _schema = Schema.Parse(@"{
        ""type"": ""record"",
        ""name"": ""LeadEnrichmentCompensated"",
        ""namespace"": ""AvroSchemas.Messages.EnrichmentEvents"",
        ""fields"": [
            { ""name"": ""EventId"", ""type"": { ""type"": ""string"", ""logicalType"": ""uuid"" } },
            { ""name"": ""OccurredOnUtc"", ""type"": ""long"" },
            { ""name"": ""EventType"", ""type"": [""null"", ""string""] },
            { ""name"": ""SchemaVersion"", ""type"": ""int"" },
            { ""name"": ""LeadId"", ""type"": { ""type"": ""string"", ""logicalType"": ""uuid"" } },
            { ""name"": ""Compensated"", ""type"": ""boolean"" }
        ]
    }");

    public override Schema Schema => _schema;

    public Guid LeadId { get; set; }
    public bool Compensated { get; set; } = true;

    public override object? Get(int fieldPos)
    {
        return fieldPos switch
        {
            0 => EventId.ToString(),
            1 => OccurredOnUtc,
            2 => EventType,
            3 => SchemaVersion,
            4 => LeadId.ToString(),
            5 => Compensated,
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
            case 5: Compensated = Convert.ToBoolean(fieldValue); break;
            default: throw new AvroRuntimeException($"Invalid field position: {fieldPos}");
        }
    }
}