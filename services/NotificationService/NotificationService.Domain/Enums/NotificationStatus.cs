namespace NotificationService.Domain.Enums;

/// <summary>
/// Статусы отправки уведомлений
/// </summary>
public enum NotificationStatus
{
    /// <summary>
    /// Ожидает отправки
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Успешно отправлено
    /// </summary>
    Sent = 2,

    /// <summary>
    /// Ошибка отправки
    /// </summary>
    Failed = 3
}