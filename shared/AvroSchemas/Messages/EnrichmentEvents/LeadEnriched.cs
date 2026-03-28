using Avro;
using AvroSchemas.Messages.Base;

// ReSharper disable InconsistentNaming

namespace AvroSchemas.Messages.EnrichmentEvents;

/// <summary>
/// Avro-событие, публикуемое после успешного обогащения лида данными
/// </summary>
public class LeadEnriched : IntegrationEventAvro
{
    private static readonly Schema _schema = Schema.Parse(@"{
        ""type"": ""record"",
        ""name"": ""LeadEnriched"",
        ""namespace"": ""AvroSchemas.Messages.EnrichmentEvents"",
        ""fields"": [
            { ""name"": ""EventId"", ""type"": { ""type"": ""string"", ""logicalType"": ""uuid"" } },
            { ""name"": ""OccurredOnUtc"", ""type"": ""long"" },
            { ""name"": ""EventType"", ""type"": [""null"", ""string""] },
            { ""name"": ""SchemaVersion"", ""type"": ""int"" },
            { ""name"": ""LeadId"", ""type"": { ""type"": ""string"", ""logicalType"": ""uuid"" } },
            { ""name"": ""Industry"", ""type"": ""string"" },
            { ""name"": ""CompanySize"", ""type"": ""string"" },
            { ""name"": ""Website"", ""type"": [""null"", ""string""] },
            { ""name"": ""RevenueRange"", ""type"": [""null"", ""string""] },
            { ""name"": ""Version"", ""type"": ""int"" }
        ]
    }");

    public override Schema Schema => _schema;

    public Guid LeadId { get; set; }
    public string Industry { get; set; } = string.Empty;
    public string CompanySize { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string? RevenueRange { get; set; }
    public int Version { get; set; }

    public override object? Get(int fieldPos)
    {
        return fieldPos switch
        {
            0 => EventId.ToString(),
            1 => OccurredOnUtc,
            2 => EventType,
            3 => SchemaVersion,
            4 => LeadId.ToString(),
            5 => Industry,
            6 => CompanySize,
            7 => Website,
            8 => RevenueRange,
            9 => Version,
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
            case 5: Industry = fieldValue.ToString() ?? string.Empty; break;
            case 6: CompanySize = fieldValue.ToString() ?? string.Empty; break;
            case 7: Website = fieldValue as string; break;
            case 8: RevenueRange = fieldValue as string; break;
            case 9: Version = Convert.ToInt32(fieldValue); break;
            default: throw new AvroRuntimeException($"Invalid field position: {fieldPos}");
        }
    }
}