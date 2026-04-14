namespace DistributionService.Domain.Enums;

/// <summary>
/// Статусы запроса на распределение лида
/// </summary>
public enum DistributionRequestStatus
{
    /// <summary>
    /// Ожидает обработки
    /// </summary>
    Pending = 1,

    /// <summary>
    /// В процессе обработки
    /// </summary>
    Processing = 2,

    /// <summary>
    /// Успешно завершен
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Завершен с ошибкой
    /// </summary>
    Failed = 4
}