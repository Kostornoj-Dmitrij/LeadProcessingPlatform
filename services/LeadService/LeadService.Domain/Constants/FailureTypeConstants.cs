namespace LeadService.Domain.Constants;

/// <summary>
/// Константы для типов ошибок при отклонении лида
/// </summary>
public static class FailureTypeConstants
{
    public const string EnrichmentFailed = "EnrichmentFailed";
    public const string ScoringFailed = "ScoringFailed";
}