using SharedKernel.Events;

namespace ScoringService.Domain.Events;

/// <summary>
/// Доменное событие - лид успешно прошел скоринг
/// </summary>
public class LeadScoredDomainEvent(
    Guid leadId,
    int totalScore,
    int qualifiedThreshold,
    List<string> appliedRules) : DomainEvent
{
    public Guid LeadId { get; } = leadId;
    public int TotalScore { get; } = totalScore;
    public int QualifiedThreshold { get; } = qualifiedThreshold;
    public List<string> AppliedRules { get; } = appliedRules;
}