namespace DistributionService.Domain.Enums;

/// <summary>
/// Статусы распределения лида
/// </summary>
public enum DistributionStatus
{
    /// <summary>
    /// Успешно распределен
    /// </summary>
    Success = 1,

    /// <summary>
    /// Ошибка распределения
    /// </summary>
    Failed = 2
}