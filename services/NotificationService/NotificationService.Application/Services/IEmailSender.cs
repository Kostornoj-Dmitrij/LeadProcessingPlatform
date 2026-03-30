namespace NotificationService.Application.Services;

/// <summary>
/// Интерфейс для отправки email уведомлений
/// </summary>
public interface IEmailSender
{
    Task<bool> SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}