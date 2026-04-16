namespace NotificationService.Domain.Constants;

/// <summary>
/// Константы для статусов уведомлений в событиях
/// </summary>
public static class NotificationStatusConstants
{
    public const string Sent = "Sent";
    public static string FailedPermanently(string reason) => $"Failed permanently: {reason}";
}