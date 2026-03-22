using SharedKernel.Base;
using EnrichmentService.Domain.Events;
using SharedKernel.Events;

namespace EnrichmentService.Domain.Entities;

/// <summary>
/// Агрегат для хранения результатов успешного обогащения лида
/// </summary>
public class EnrichmentResult : Entity<Guid>, IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid LeadId { get; private set; }
    public string CompanyName { get; private set; }
    public string Industry { get; private set; }
    public string CompanySize { get; private set; }
    public string? Website { get; private set; }
    public string? RevenueRange { get; private set; }
    public string? RawResponse { get; private set; }
    public DateTime EnrichedAt { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private EnrichmentResult(Guid id) : base(id) { }
    private EnrichmentResult() { }

    public static EnrichmentResult Create(
        Guid leadId,
        string companyName,
        string industry,
        string companySize,
        string? website = null,
        string? revenueRange = null,
        string? rawResponse = null)
    {
        if (leadId == Guid.Empty)
            throw new ArgumentException("LeadId cannot be empty", nameof(leadId));

        var result = new EnrichmentResult(Guid.NewGuid())
        {
            LeadId = leadId,
            CompanyName = companyName,
            Industry = industry,
            CompanySize = companySize,
            Website = website,
            RevenueRange = revenueRange,
            RawResponse = rawResponse,
            EnrichedAt = DateTime.UtcNow
        };

        result.AddDomainEvent(new LeadEnrichedDomainEvent(leadId, industry, companySize, website, revenueRange, 1));

        return result;
    }

    private void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}