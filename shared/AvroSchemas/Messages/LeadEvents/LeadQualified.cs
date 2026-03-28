using Avro;
using AvroSchemas.Messages.Base;

// ReSharper disable InconsistentNaming

namespace AvroSchemas.Messages.LeadEvents;

/// <summary>
/// Avro-событие, публикуемое при успешной квалификации лида
/// </summary>
public class LeadQualified : IntegrationEventAvro
{
    private static readonly Schema _schema = Schema.Parse(@"{
        ""type"": ""record"",
        ""name"": ""LeadQualified"",
        ""namespace"": ""AvroSchemas.Messages.LeadEvents"",
        ""fields"": [
            { ""name"": ""EventId"", ""type"": { ""type"": ""string"", ""logicalType"": ""uuid"" } },
            { ""name"": ""OccurredOnUtc"", ""type"": ""long"" },
            { ""name"": ""EventType"", ""type"": [""null"", ""string""] },
            { ""name"": ""SchemaVersion"", ""type"": ""int"" },
            { ""name"": ""LeadId"", ""type"": { ""type"": ""string"", ""logicalType"": ""uuid"" } },
            { ""name"": ""CompanyName"", ""type"": ""string"" },
            { ""name"": ""ContactPerson"", ""type"": [""null"", ""string""] },
            { ""name"": ""Email"", ""type"": ""string"" },
            { ""name"": ""Score"", ""type"": ""int"" },
            { 
                ""name"": ""EnrichedData"", 
                ""type"": [""null"", {
                    ""type"": ""record"",
                    ""name"": ""EnrichedData"",
                    ""fields"": [
                        { ""name"": ""Industry"", ""type"": ""string"" },
                        { ""name"": ""CompanySize"", ""type"": ""string"" },
                        { ""name"": ""Website"", ""type"": [""null"", ""string""] },
                        { ""name"": ""RevenueRange"", ""type"": [""null"", ""string""] },
                        { ""name"": ""Version"", ""type"": ""int"" }
                    ]
                }]
            }
        ]
    }");

    public override Schema Schema => _schema;

    public Guid LeadId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string Email { get; set; } = string.Empty;
    public int Score { get; set; }
    public EnrichedData? EnrichedData { get; set; }

    public override object? Get(int fieldPos)
    {
        return fieldPos switch
        {
            0 => EventId.ToString(),
            1 => OccurredOnUtc,
            2 => EventType,
            3 => SchemaVersion,
            4 => LeadId.ToString(),
            5 => CompanyName,
            6 => ContactPerson,
            7 => Email,
            8 => Score,
            9 => EnrichedData,
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
            case 5: CompanyName = fieldValue.ToString() ?? string.Empty; break;
            case 6: ContactPerson = fieldValue as string; break;
            case 7: Email = fieldValue.ToString() ?? string.Empty; break;
            case 8: Score = Convert.ToInt32(fieldValue); break;
            case 9: EnrichedData = fieldValue as EnrichedData; break;
            default: throw new AvroRuntimeException($"Invalid field position: {fieldPos}");
        }
    }
}