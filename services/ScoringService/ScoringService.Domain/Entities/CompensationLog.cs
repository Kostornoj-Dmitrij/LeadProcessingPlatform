using ScoringService.Domain.Constants;
using ScoringService.Domain.Events;
using SharedKernel.Base;
using SharedKernel.Events;

namespace ScoringService.Domain.Entities;

/// <summary>
/// Агрегат для логирования факта выполнения компенсации скоринга
/// </summary>
public class CompensationLog : Entity<Guid>, IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid LeadId { get; private set; }
    public string CompensationType { get; private set; }
    public string? Reason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public bool IsCompensated { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private CompensationLog(Guid id) : base(id) { }

    public static CompensationLog Create(Guid leadId, string compensationType, string? reason = null)
    {
        var log = new CompensationLog(Guid.NewGuid())
        {
            LeadId = leadId,
            CompensationType = compensationType,
            Reason = reason,
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = null,
            IsCompensated = false
        };
        return log;
    }

    public static CompensationLog CreateScoringCompensation(Guid leadId, string? reason = null)
    {
        return Create(leadId, CompensationConstants.ScoringCompensated, reason);
    }

    public void MarkCompensated()
    {
        IsCompensated = true;
        ProcessedAt = DateTime.UtcNow;
        AddDomainEvent(new LeadScoringCompensatedDomainEvent(LeadId));
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