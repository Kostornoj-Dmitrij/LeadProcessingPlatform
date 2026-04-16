namespace DistributionService.Domain.Constants;

/// <summary>
/// Константы для типов правил распределения
/// </summary>
public static class RuleTypeConstants
{
    public const string ScoreThreshold = "score_threshold";
    public const string IndustryMatch = "industry_match";
    public const string RevenueRange = "revenue_range";
    public const string AlwaysTrue = "always_true";
}