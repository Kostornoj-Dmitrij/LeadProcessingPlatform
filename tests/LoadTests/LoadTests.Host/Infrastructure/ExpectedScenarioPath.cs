namespace LoadTests.Host.Infrastructure;

/// <summary>
/// Ожидаемый путь прохождения лида по статусам
/// </summary>
public enum ExpectedScenarioPath
{
    /// <summary>
    /// Успешный сценарий: Initial - Qualified - Distributed - Closed
    /// </summary>
    Success,

    /// <summary>
    /// Ошибка обогащения: Initial - Rejected - Closed
    /// </summary>
    EnrichmentFailure,

    /// <summary>
    /// Ошибка скоринга: Initial - Rejected - Closed
    /// </summary>
    ScoringFailure,

    /// <summary>
    /// Ошибка распределения: Initial - Qualified - FailedDistribution - Closed
    /// </summary>
    DistributionFailure
}