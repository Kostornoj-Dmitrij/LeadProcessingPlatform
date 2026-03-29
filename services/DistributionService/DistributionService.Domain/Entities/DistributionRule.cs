using SharedKernel.Base;
using DistributionService.Domain.Enums;

namespace DistributionService.Domain.Entities;

/// <summary>
/// Агрегат для хранения правил распределения лидов
/// </summary>
public class DistributionRule : Entity<Guid>, IAggregateRoot
{
    public string RuleName { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public DistributionRuleStrategy Strategy { get; private set; }

    public string ConditionJson { get; private set; } = string.Empty;

    public string TargetConfigJson { get; private set; } = string.Empty;

    public int Priority { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime ValidFrom { get; private set; }

    public DateTime? ValidTo { get; private set; }

    public int Version { get; private set; }

    private DistributionRule(Guid id) : base(id) { }

    public static DistributionRule Create(
        Guid id,
        string ruleName,
        DistributionRuleStrategy strategy,
        string conditionJson,
        string targetConfigJson,
        int priority = 0)
    {
        return new DistributionRule(id)
        {
            RuleName = ruleName,
            Strategy = strategy,
            ConditionJson = conditionJson,
            TargetConfigJson = targetConfigJson,
            Priority = priority,
            IsActive = true,
            ValidFrom = DateTime.UtcNow,
            Version = 1
        };
    }

    public void Deactivate()
    {
        IsActive = false;
        ValidTo = DateTime.UtcNow;
    }

    public void UpdatePriority(int newPriority)
    {
        Priority = newPriority;
    }

    public bool IsApplicable(DateTime currentTime)
    {
        return IsActive && ValidFrom <= currentTime && (!ValidTo.HasValue || ValidTo.Value > currentTime);
    }
}