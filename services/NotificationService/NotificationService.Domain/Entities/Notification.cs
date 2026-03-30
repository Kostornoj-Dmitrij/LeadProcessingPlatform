using NotificationService.Domain.Enums;
using NotificationService.Domain.Events;
using SharedKernel.Base;
using SharedKernel.Events;

namespace NotificationService.Domain.Entities;

/// <summary>
/// Агрегат для хранения отправленных уведомлений
/// </summary>
public class Notification : Entity<Guid>, IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid LeadId { get; private set; }
    public string EventId { get; private set; } = string.Empty;
    public string NotificationType { get; private set; } = string.Empty;
    public NotificationChannel Channel { get; private set; }
    public string Recipient { get; private set; } = string.Empty;
    public string? Subject { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public NotificationStatus Status { get; private set; }
    public DateTime? SentAt { get; private set; }
    public string? FailureReason { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime? NextRetryAt { get; private set; }
    public int MaxRetries { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private Notification(Guid id) : base(id) { }

    public static Notification Create(
        Guid leadId,
        string eventId,
        string notificationType,
        NotificationChannel channel,
        string recipient,
        string body,
        string? subject = null,
        int maxRetries = 3)
    {
        var notification = new Notification(Guid.NewGuid())
        {
            LeadId = leadId,
            EventId = eventId,
            NotificationType = notificationType,
            Channel = channel,
            Recipient = recipient,
            Subject = subject,
            Body = body,
            Status = NotificationStatus.Pending,
            RetryCount = 0,
            MaxRetries = maxRetries,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return notification;
    }

    public void MarkAsSent()
    {
        if (Status != NotificationStatus.Pending)
            throw new InvalidOperationException($"Cannot mark as sent when status is {Status}");

        Status = NotificationStatus.Sent;
        SentAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        NextRetryAt = null;

        AddDomainEvent(new NotificationSentDomainEvent(LeadId, NotificationType, Channel.ToString(), "Sent"));
    }

    public void MarkAsFailed(string reason, DateTime? nextRetryAt = null)
    {
        Status = NotificationStatus.Failed;
        FailureReason = reason;
        RetryCount++;
        UpdatedAt = DateTime.UtcNow;
        NextRetryAt = nextRetryAt;

        if (RetryCount >= MaxRetries)
        {
            AddDomainEvent(new NotificationSentDomainEvent(LeadId, NotificationType, Channel.ToString(),
                $"Failed permanently: {reason}"));
        }
    }

    private bool CanRetry()
    {
        return RetryCount < MaxRetries;
    }

    public bool IsReadyForRetry()
    {
        return Status == NotificationStatus.Failed && 
               CanRetry() && 
               (!NextRetryAt.HasValue || NextRetryAt.Value <= DateTime.UtcNow);
    }

    private void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}