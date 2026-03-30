using AvroSchemas;
using AvroSchemas.Messages.NotificationEvents;
using SharedKernel.Events;

namespace NotificationService.Domain.Events;

/// <summary>
/// Доменное событие - уведомление отправлено
/// </summary>
public class NotificationSentDomainEvent(
    Guid leadId,
    string notificationType,
    string channel,
    string status) : DomainEvent
{
    public Guid LeadId { get; } = leadId;
    public string NotificationType { get; } = notificationType;
    public string Channel { get; } = channel;
    public string Status { get; } = status;

    public override IIntegrationEvent ToIntegrationEvent()
    {
        return new NotificationSent
        {
            EventId = EventId,
            OccurredOnUtc = new DateTimeOffset(OccurredOn).ToUnixTimeMilliseconds(),
            EventType = GetType().Name,
            SchemaVersion = 1,
            LeadId = LeadId,
            NotificationType = NotificationType,
            Channel = Channel,
            Status = Status
        };
    }
}