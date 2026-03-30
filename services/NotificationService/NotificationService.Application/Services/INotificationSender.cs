using NotificationService.Domain.Enums;

namespace NotificationService.Application.Services;

/// <summary>
/// Интерфейс для отправки уведомлений
/// </summary>
public interface INotificationSender
{
    Task<(bool success, string subject, string body)> SendAsync(
        string notificationType,
        NotificationChannel channel,
        string recipient,
        Dictionary<string, string> variables,
        CancellationToken cancellationToken = default);
}