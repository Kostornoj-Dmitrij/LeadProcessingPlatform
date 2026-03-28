using AvroSchemas;
using AvroSchemas.Messages.EnrichmentEvents;
using SharedKernel.Events;

namespace EnrichmentService.Domain.Events;

/// <summary>
/// Доменное событие - лид успешно обогащен данными
/// </summary>
public class LeadEnrichedDomainEvent(
    Guid leadId,
    string industry,
    string companySize,
    string? website,
    string? revenueRange,
    int version) : DomainEvent
{
    public Guid LeadId { get; } = leadId;
    public string Industry { get; } = industry;
    public string CompanySize { get; } = companySize;
    public string? Website { get; } = website;
    public string? RevenueRange { get; } = revenueRange;
    public int Version { get; } = version;

    public override IIntegrationEvent ToIntegrationEvent()
    {
        return new LeadEnriched
        {
            EventId = EventId,
            OccurredOnUtc = new DateTimeOffset(OccurredOn).ToUnixTimeMilliseconds(),
            EventType = GetType().Name,
            SchemaVersion = 1,
            LeadId = LeadId,
            Industry = Industry,
            CompanySize = CompanySize,
            Website = Website,
            RevenueRange = RevenueRange,
            Version = Version
        };
    }
}