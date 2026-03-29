namespace DistributionService.Domain.Enums;

/// <summary>
/// Стратегии распределения лидов
/// </summary>
public enum DistributionRuleStrategy
{
    /// <summary>
    /// Распределение по кругу
    /// </summary>
    RoundRobin = 1,

    /// <summary>
    /// Распределение по территории
    /// </summary>
    Territory = 2,

    /// <summary>
    /// Распределение по специализации
    /// </summary>
    Specialization = 3,

    /// <summary>
    /// Распределение на основе оценки
    /// </summary>
    ScoreBased = 4,

    /// <summary>
    /// Распределение в фиксированную систему
    /// </summary>
    FixedTarget = 5
}