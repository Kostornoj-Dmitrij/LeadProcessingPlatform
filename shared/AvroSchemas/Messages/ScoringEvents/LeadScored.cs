using Avro;
using AvroSchemas.Messages.Base;

// ReSharper disable InconsistentNaming

namespace AvroSchemas.Messages.ScoringEvents;

/// <summary>
/// Avro-событие, публикуемое после успешного выполнения скоринга лида
/// </summary>
public class LeadScored : IntegrationEventAvro
{
    private static readonly Schema _schema = Schema.Parse(@"{
        ""type"": ""record"",
        ""name"": ""LeadScored"",
        ""namespace"": ""AvroSchemas.Messages.ScoringEvents"",
        ""fields"": [
            { ""name"": ""EventId"", ""type"": { ""type"": ""string"", ""logicalType"": ""uuid"" } },
            { ""name"": ""OccurredOnUtc"", ""type"": ""long"" },
            { ""name"": ""EventType"", ""type"": [""null"", ""string""] },
            { ""name"": ""SchemaVersion"", ""type"": ""int"" },
            { ""name"": ""LeadId"", ""type"": { ""type"": ""string"", ""logicalType"": ""uuid"" } },
            { ""name"": ""TotalScore"", ""type"": ""int"" },
            { ""name"": ""QualifiedThreshold"", ""type"": ""int"" },
            { ""name"": ""AppliedRules"", ""type"": [""null"", { ""type"": ""array"", ""items"": ""string"" }] }
        ]
    }");

    public override Schema Schema => _schema;

    public Guid LeadId { get; set; }
    public int TotalScore { get; set; }
    public int QualifiedThreshold { get; set; }
    public List<string>? AppliedRules { get; set; }

    public override object? Get(int fieldPos)
    {
        return fieldPos switch
        {
            0 => EventId.ToString(),
            1 => OccurredOnUtc,
            2 => EventType,
            3 => SchemaVersion,
            4 => LeadId.ToString(),
            5 => TotalScore,
            6 => QualifiedThreshold,
            7 => AppliedRules,
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
            case 5: TotalScore = Convert.ToInt32(fieldValue); break;
            case 6: QualifiedThreshold = Convert.ToInt32(fieldValue); break;
            case 7: AppliedRules = fieldValue as List<string>; break;
            default: throw new AvroRuntimeException($"Invalid field position: {fieldPos}");
        }
    }
}