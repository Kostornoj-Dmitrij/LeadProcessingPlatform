using Avro;
using AvroSchemas.Messages.Base;

// ReSharper disable InconsistentNaming

namespace AvroSchemas.Messages.LeadEvents;

/// <summary>
/// Avro-событие, публикуемое при создании нового лида
/// </summary>
public class LeadCreated : IntegrationEventAvro
{
    private static readonly Schema _schema = Schema.Parse(@"{
        ""type"": ""record"",
        ""name"": ""LeadCreated"",
        ""namespace"": ""AvroSchemas.Messages.LeadEvents"",
        ""fields"": [
            { ""name"": ""EventId"", ""type"": { ""type"": ""string"", ""logicalType"": ""uuid"" } },
            { ""name"": ""OccurredOnUtc"", ""type"": ""long"" },
            { ""name"": ""EventType"", ""type"": [""null"", ""string""] },
            { ""name"": ""SchemaVersion"", ""type"": ""int"" },
            { ""name"": ""LeadId"", ""type"": { ""type"": ""string"", ""logicalType"": ""uuid"" } },
            { ""name"": ""ExternalLeadId"", ""type"": [""null"", ""string""] },
            { ""name"": ""Source"", ""type"": ""string"" },
            { ""name"": ""CompanyName"", ""type"": ""string"" },
            { ""name"": ""ContactPerson"", ""type"": [""null"", ""string""] },
            { ""name"": ""Email"", ""type"": ""string"" },
            { ""name"": ""Phone"", ""type"": [""null"", ""string""] },
            { ""name"": ""CustomFields"", ""type"": [""null"", { ""type"": ""map"", ""values"": ""string"" }] }
        ]
    }");

    public override Schema Schema => _schema;

    public Guid LeadId { get; set; }
    public string? ExternalLeadId { get; set; }
    public string Source { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public Dictionary<string, string>? CustomFields { get; set; }

    public override object? Get(int fieldPos)
    {
        return fieldPos switch
        {
            0 => EventId.ToString(),
            1 => OccurredOnUtc,
            2 => EventType,
            3 => SchemaVersion,
            4 => LeadId.ToString(),
            5 => ExternalLeadId,
            6 => Source,
            7 => CompanyName,
            8 => ContactPerson,
            9 => Email,
            10 => Phone,
            11 => CustomFields,
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
            case 5: ExternalLeadId = fieldValue as string; break;
            case 6: Source = fieldValue.ToString() ?? string.Empty; break;
            case 7: CompanyName = fieldValue.ToString() ?? string.Empty; break;
            case 8: ContactPerson = fieldValue as string; break;
            case 9: Email = fieldValue.ToString() ?? string.Empty; break;
            case 10: Phone = fieldValue as string; break;
            case 11: CustomFields = fieldValue as Dictionary<string, string>; break;
            default: throw new AvroRuntimeException($"Invalid field position: {fieldPos}");
        }
    }
}