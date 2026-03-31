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
    public DateTime? NextRetryAt { get; private set; }
    public string? TraceParent { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private EnrichmentRequest(Guid id) : base(id) { }

    public static EnrichmentRequest Create(Guid leadId, string companyName, string email,
        string? contactPerson, Dictionary<string, string>? customFields, string? traceParent = null)
    {
        return new EnrichmentRequest(Guid.NewGuid())
        {
            LeadId = leadId,
            CompanyName = companyName,
            Email = email,
            ContactPerson = contactPerson,
            CustomFields = customFields,
            TraceParent = traceParent,
            Status = EnrichmentRequestStatus.Pending,
            RetryCount = 0,
            NextRetryAt = null
        };
    }

    public void StartProcessing()
    {
        if (Status != EnrichmentRequestStatus.Pending && Status != EnrichmentRequestStatus.Failed)
        {
            throw new InvalidOperationException(
                $"Cannot start processing an enrichment request that is not in Pending or Failed state. " +
                $"Current state: {Status}");
        }

        Status = EnrichmentRequestStatus.Processing;
        LastAttemptAt = DateTime.UtcNow;
    }

    public void MarkCompleted()
    {
        if (Status != EnrichmentRequestStatus.Processing)
        {
            throw new InvalidOperationException(
                $"Cannot complete an enrichment request that is not in Processing state. " +
                $"Current state: {Status}");
        }

        Status = EnrichmentRequestStatus.Completed;
        LastAttemptAt = DateTime.UtcNow;
        NextRetryAt = null;
    }

    public void MarkFailed(string errorMessage, DateTime? nextRetryAt = null)
    {
        if (Status != EnrichmentRequestStatus.Processing)
        {
            throw new InvalidOperationException(
                $"Cannot mark as failed an enrichment request that is not in Processing state. " +
                $"Current state: {Status}");
        }

        Status = EnrichmentRequestStatus.Failed;
        LastAttemptAt = DateTime.UtcNow;
        ErrorMessage = errorMessage;
        RetryCount++;
        NextRetryAt = nextRetryAt;
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