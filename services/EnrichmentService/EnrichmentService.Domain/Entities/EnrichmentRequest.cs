using SharedKernel.Base;
using EnrichmentService.Domain.Events;
using EnrichmentService.Domain.Enums;
using SharedKernel.Events;

namespace EnrichmentService.Domain.Entities;

/// <summary>
/// Агрегат для управления запросами на обогащение лидов
/// </summary>
public class EnrichmentRequest : Entity<Guid>, IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid LeadId { get; private set; }
    public string CompanyName { get; private set; } = string.Empty;
    public string? ContactPerson { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public Dictionary<string, string>? CustomFields { get; private set; }
    public EnrichmentRequestStatus Status { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime? LastAttemptAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private EnrichmentRequest(Guid id) : base(id) { }

    public static EnrichmentRequest Create(Guid leadId, string companyName, string email,
        string? contactPerson, Dictionary<string, string>? customFields)
    {
        return new EnrichmentRequest(Guid.NewGuid())
        {
            LeadId = leadId,
            CompanyName = companyName,
            Email = email,
            ContactPerson = contactPerson,
            CustomFields = customFields,
            Status = EnrichmentRequestStatus.Pending,
            RetryCount = 0
        };
    }

    public void StartProcessing()
    {
        Status = EnrichmentRequestStatus.Processing;
        LastAttemptAt = DateTime.UtcNow;
    }

    public void MarkCompleted(string industry, string companySize, string? website, string? revenueRange, string? rawResponse)
    {
        Status = EnrichmentRequestStatus.Completed;
        LastAttemptAt = DateTime.UtcNow;
        AddDomainEvent(new LeadEnrichedDomainEvent(LeadId, industry, companySize, website, revenueRange, 1));
    }

    public void MarkFailed(string errorMessage)
    {
        Status = EnrichmentRequestStatus.Failed;
        LastAttemptAt = DateTime.UtcNow;
        ErrorMessage = errorMessage;
        RetryCount++;
        AddDomainEvent(new LeadEnrichmentFailedDomainEvent(LeadId, errorMessage, RetryCount));
    }

    private void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public bool CanRetry(int maxRetries) => RetryCount < maxRetries;

    public bool IsReadyForProcessing(int maxRetries) =>
        Status == EnrichmentRequestStatus.Pending ||
        (Status == EnrichmentRequestStatus.Failed && CanRetry(maxRetries));
}