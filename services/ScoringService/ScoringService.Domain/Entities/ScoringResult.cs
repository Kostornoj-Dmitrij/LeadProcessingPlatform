using SharedKernel.Base;

namespace ScoringService.Domain.Entities;

/// <summary>
/// Агрегат для хранения результатов успешного скоринга лида
/// </summary>
public class ScoringResult : Entity<Guid>, IAggregateRoot
{
    public Guid LeadId { get; private set; }
    public int TotalScore { get; private set; }
    public int QualifiedThreshold { get; private set; }
    public string AppliedRulesJson { get; private set; } = string.Empty;
    public DateTime CalculatedAt { get; private set; }

    private ScoringResult(Guid id) : base(id) { }

    public static ScoringResult Create(Guid leadId, int totalScore, int qualifiedThreshold, List<string> appliedRules)
    {
        return new ScoringResult(Guid.NewGuid())
        {
            LeadId = leadId,
            TotalScore = totalScore,
            QualifiedThreshold = qualifiedThreshold,
            AppliedRulesJson = System.Text.Json.JsonSerializer.Serialize(appliedRules),
            CalculatedAt = DateTime.UtcNow
        };
    }
}