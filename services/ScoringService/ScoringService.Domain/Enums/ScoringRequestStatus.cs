namespace ScoringService.Domain.Enums;

/// <summary>
/// Статусы запроса на скоринг лида
/// </summary>
public enum ScoringRequestStatus
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