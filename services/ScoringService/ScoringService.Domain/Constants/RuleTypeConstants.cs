namespace ScoringService.Domain.Constants;

/// <summary>
/// Константы для типов правил скоринга
/// </summary>
public static class RuleTypeConstants
{
    public const string AlwaysTrue = "always_true";
    public const string FieldEquals = "field_equals";
    public const string FieldContains = "field_contains";
    public const string CustomFieldEquals = "custom_field_equals";
    public const string ScoreThreshold = "score_threshold";
}