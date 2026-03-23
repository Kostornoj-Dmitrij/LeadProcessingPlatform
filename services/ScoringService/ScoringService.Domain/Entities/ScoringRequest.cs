using ScoringService.Domain.Enums;
using ScoringService.Domain.Events;
using SharedKernel.Base;
using SharedKernel.Events;

namespace ScoringService.Domain.Entities;

/// <summary>
/// Агрегат для управления запросами на скоринг лидов
/// </summary>
public class ScoringRequest : Entity<Guid>, IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid LeadId { get; private set; }
    public string CompanyName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? ContactPerson { get; private set; }
    public Dictionary<string, string>? CustomFields { get; private set; }
    public string? EnrichedData { get; private set; }
    public ScoringRequestStatus Status { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime? LastAttemptAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private ScoringRequest(Guid id) : base(id) { }

    public static ScoringRequest Create(
        Guid leadId, 
        string companyName, 
        string email,
        string? contactPerson = null,
        Dictionary<string, string>? customFields = null,
        string? enrichedData = null)
    {
        var request = new ScoringRequest(Guid.NewGuid())
        {
            LeadId = leadId,
            CompanyName = companyName,
            Email = email,
            ContactPerson = contactPerson,
            CustomFields = customFields,
            EnrichedData = enrichedData,
            Status = ScoringRequestStatus.Pending,
            RetryCount = 0
        };
        return request;
    }

    public void UpdateEnrichedData(string enrichedDataJson)
    {
        if (Status != ScoringRequestStatus.Pending && Status != ScoringRequestStatus.Failed)
        {
            throw new InvalidOperationException(
                $"Cannot update enriched data when request is in {Status} state. " +
                $"Only Pending or Failed states are allowed.");
        }

        EnrichedData = enrichedDataJson;
    }

    public void StartProcessing()
    {
        if (Status != ScoringRequestStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Cannot start processing a scoring request that is not in Pending state. " +
                $"Current state: {Status}");
        }

        Status = ScoringRequestStatus.Processing;
        LastAttemptAt = DateTime.UtcNow;
    }

    public void MarkCompleted(int totalScore, int qualifiedThreshold, List<string> appliedRules)
    {
        if (Status != ScoringRequestStatus.Processing)
        {
            throw new InvalidOperationException(
                $"Cannot complete a scoring request that is not in Processing state. " +
                $"Current state: {Status}");
        }

        Status = ScoringRequestStatus.Completed;
        LastAttemptAt = DateTime.UtcNow;
        AddDomainEvent(new LeadScoredDomainEvent(LeadId, totalScore, qualifiedThreshold, appliedRules));
    }

    public void MarkFailed(string errorMessage)
    {
        if (Status != ScoringRequestStatus.Processing)
        {
            throw new InvalidOperationException(
                $"Cannot mark as failed a scoring request that is not in Processing state. " +
                $"Current state: {Status}");
        }

        Status = ScoringRequestStatus.Failed;
        LastAttemptAt = DateTime.UtcNow;
        ErrorMessage = errorMessage;
        RetryCount++;
        AddDomainEvent(new LeadScoringFailedDomainEvent(LeadId, errorMessage, RetryCount));
    }

    private void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();

    public bool CanRetry(int maxRetries) => RetryCount < maxRetries;

    public void ClearEnrichedData()
    {
        EnrichedData = null;
    }

    public bool IsReadyForProcessing(int maxRetries) =>
        Status == ScoringRequestStatus.Pending ||
        (Status == ScoringRequestStatus.Failed && CanRetry(maxRetries));
}