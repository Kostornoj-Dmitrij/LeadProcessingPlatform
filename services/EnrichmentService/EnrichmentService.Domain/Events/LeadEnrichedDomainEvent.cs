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
    int version)
    : DomainEvent
{
    public Guid LeadId { get; } = leadId;

    public string Industry { get; } = industry;

    public string CompanySize { get; } = companySize;

    public string? Website { get; } = website;

    public string? RevenueRange { get; } = revenueRange;

    public int Version { get; } = version;
}