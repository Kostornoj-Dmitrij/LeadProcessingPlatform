using IntegrationEvents.Base;

namespace IntegrationEvents.NotificationEvents;

/// <summary>
/// Публикуется Notification Service после отправки уведомления.
/// </summary>
public class NotificationSentIntegrationEvent : IntegrationEvent
{
    public Guid LeadId { get; set; }

    public string NotificationType { get; set; } = string.Empty;

    public string Channel { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}