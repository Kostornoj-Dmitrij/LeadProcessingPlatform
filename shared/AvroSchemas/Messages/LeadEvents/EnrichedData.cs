using Avro;
using Avro.Specific;

// ReSharper disable InconsistentNaming

namespace AvroSchemas.Messages.LeadEvents;

/// <summary>
/// Avro-запись, представляющая обогащенные данные о компании
/// </summary>
public class EnrichedData : ISpecificRecord
{
    private static readonly Schema _schema = Schema.Parse(@"{
        ""type"": ""record"",
        ""name"": ""EnrichedData"",
        ""namespace"": ""AvroSchemas.Messages.LeadEvents"",
        ""fields"": [
            { ""name"": ""Industry"", ""type"": ""string"" },
            { ""name"": ""CompanySize"", ""type"": ""string"" },
            { ""name"": ""Website"", ""type"": [""null"", ""string""] },
            { ""name"": ""RevenueRange"", ""type"": [""null"", ""string""] },
            { ""name"": ""Version"", ""type"": ""int"" }
        ]
    }");

    public Schema Schema => _schema;

    public string Industry { get; set; } = string.Empty;
    public string CompanySize { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string? RevenueRange { get; set; }
    public int Version { get; set; }

    public object? Get(int fieldPos)
    {
        return fieldPos switch
        {
            0 => Industry,
            1 => CompanySize,
            2 => Website,
            3 => RevenueRange,
            4 => Version,
            _ => throw new AvroRuntimeException($"Invalid field position: {fieldPos}")
        };
    }

    public void Put(int fieldPos, object fieldValue)
    {
        switch (fieldPos)
        {
            case 0: Industry = fieldValue.ToString() ?? string.Empty; break;
            case 1: CompanySize = fieldValue.ToString() ?? string.Empty; break;
            case 2: Website = fieldValue as string; break;
            case 3: RevenueRange = fieldValue as string; break;
            case 4: Version = Convert.ToInt32(fieldValue); break;
            default: throw new AvroRuntimeException($"Invalid field position: {fieldPos}");
        }
    }

    public static EnrichedData? FromDto(EnrichedDataDto? dto)
    {
        if (dto == null) return null;
        return new EnrichedData
        {
            Industry = dto.Industry,
            CompanySize = dto.CompanySize,
            Website = dto.Website,
            RevenueRange = dto.RevenueRange,
            Version = dto.Version
        };
    }

    public EnrichedDataDto ToDto()
    {
        return new EnrichedDataDto
        {
            Industry = Industry,
            CompanySize = CompanySize,
            Website = Website,
            RevenueRange = RevenueRange,
            Version = Version
        };
    }
}