using SharedKernel.Base;

namespace ScoringService.Domain.Entities;

/// <summary>
/// Агрегат для хранения правил скоринга
/// </summary>
public class ScoringRule : Entity<Guid>, IAggregateRoot
{
    public string RuleName { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string ConditionJson { get; private set; } = string.Empty;
    public int ScoreValue { get; private set; }
    public int Priority { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime ValidFrom { get; private set; }
    public DateTime? ValidTo { get; private set; }
    public int Version { get; private set; }

    private ScoringRule(Guid id) : base(id) { }

    public static ScoringRule Create(Guid id, string ruleName, string conditionJson, int scoreValue, int priority = 0)
    {
        return new ScoringRule(id)
        {
            RuleName = ruleName,
            ConditionJson = conditionJson,
            ScoreValue = scoreValue,
            Priority = priority,
            IsActive = true,
            ValidFrom = DateTime.UtcNow,
            Version = 1
        };
    }
}