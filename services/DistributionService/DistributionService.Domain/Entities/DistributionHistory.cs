using SharedKernel.Base;
using DistributionService.Domain.Enums;
using DistributionService.Domain.Events;
using SharedKernel.Events;

namespace DistributionService.Domain.Entities;

/// <summary>
/// Агрегат для хранения истории распределения лидов
/// </summary>
public class DistributionHistory : Entity<Guid>, IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid LeadId { get; private set; }

    public Guid? RuleId { get; private set; }

    public string Target { get; private set; } = string.Empty;

    public DistributionStatus Status { get; private set; }

    public string? ResponseData { get; private set; }

    public string? ErrorMessage { get; private set; }

    public DateTime DistributedAt { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private DistributionHistory(Guid id) : base(id) { }

    public static DistributionHistory CreateSuccess(
        Guid leadId,
        Guid? ruleId,
        string target,
        string? responseData = null)
    {
        var history = new DistributionHistory(Guid.NewGuid())
        {
            LeadId = leadId,
            RuleId = ruleId,
            Target = target,
            Status = DistributionStatus.Success,
            ResponseData = responseData,
            DistributedAt = DateTime.UtcNow
        };

        history.AddDomainEvent(new DistributionSucceededDomainEvent(leadId, target, history.DistributedAt));

        return history;
    }

    public static DistributionHistory CreateFailed(
        Guid leadId,
        Guid? ruleId,
        string errorMessage,
        string? attemptedTarget = null)
    {
        var history = new DistributionHistory(Guid.NewGuid())
        {
            LeadId = leadId,
            RuleId = ruleId,
            Target = attemptedTarget ?? string.Empty,
            Status = DistributionStatus.Failed,
            ErrorMessage = errorMessage,
            DistributedAt = DateTime.UtcNow
        };

        history.AddDomainEvent(new DistributionFailedDomainEvent(leadId, errorMessage));

        return history;
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