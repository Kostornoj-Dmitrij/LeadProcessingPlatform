using SharedKernel.Base;
using DistributionService.Domain.Enums;
using DistributionService.Domain.Events;
using SharedKernel.Events;

namespace DistributionService.Domain.Entities;

/// <summary>
/// Агрегат для управления запросами на распределение лидов
/// </summary>
public class DistributionRequest : Entity<Guid>, IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid LeadId { get; private set; }
    public string CompanyName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? ContactPerson { get; private set; }
    public int Score { get; private set; }
    public Dictionary<string, string>? CustomFields { get; private set; }
    public string? EnrichedData { get; private set; }
    public Guid? RuleId { get; private set; }
    public string? Target { get; private set; }
    public DistributionRequestStatus Status { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime? LastAttemptAt { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime? NextRetryAt { get; private set; }
    public string? TraceParent { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private DistributionRequest(Guid id) : base(id) { }

    public static DistributionRequest Create(
        Guid leadId,
        string companyName,
        string email,
        int score,
        string? contactPerson = null,
        Dictionary<string, string>? customFields = null,
        string? enrichedData = null,
        string? traceParent = null)
    {
        return new DistributionRequest(Guid.NewGuid())
        {
            LeadId = leadId,
            CompanyName = companyName,
            Email = email,
            Score = score,
            ContactPerson = contactPerson,
            CustomFields = customFields,
            EnrichedData = enrichedData,
            TraceParent = traceParent,
            Status = DistributionRequestStatus.Pending,
            RetryCount = 0,
            NextRetryAt = null
        };
    }

    public void SetRuleAndTarget(Guid ruleId, string target)
    {
        RuleId = ruleId;
        Target = target;
    }

    public void StartProcessing()
    {
        if (Status != DistributionRequestStatus.Pending && Status != DistributionRequestStatus.Failed)
        {
            throw new InvalidOperationException(
                $"Cannot start processing a distribution request that is not in Pending or Failed state. " +
                $"Current state: {Status}");
        }

        Status = DistributionRequestStatus.Processing;
        LastAttemptAt = DateTime.UtcNow;
    }

    public void MarkCompleted()
    {
        if (Status != DistributionRequestStatus.Processing)
        {
            throw new InvalidOperationException(
                $"Cannot complete a distribution request that is not in Processing state. " +
                $"Current state: {Status}");
        }

        Status = DistributionRequestStatus.Completed;
        LastAttemptAt = DateTime.UtcNow;
        NextRetryAt = null;
    }

    public void MarkFailed(string errorMessage, DateTime? nextRetryAt = null)
    {
        if (Status != DistributionRequestStatus.Processing)
        {
            throw new InvalidOperationException(
                $"Cannot mark as failed a distribution request that is not in Processing state. " +
                $"Current state: {Status}");
        }

        Status = DistributionRequestStatus.Failed;
        LastAttemptAt = DateTime.UtcNow;
        ErrorMessage = errorMessage;
        RetryCount++;
        NextRetryAt = nextRetryAt;
        AddDomainEvent(new DistributionFailedDomainEvent(LeadId, errorMessage));
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