using AutoFixture.NUnit4;
using NUnit.Framework;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Events;
using NotificationService.Tests.Common.Attributes;

namespace NotificationService.Tests.Domain.Entities;

/// <summary>
/// Тесты для Notification
/// </summary>
[Category("Domain")]
public class NotificationTests
{
    #region Create

    [Test, AutoData]
    public void Create_WithValidData_ShouldCreateNotification(
        Guid leadId,
        string eventId,
        string notificationType,
        NotificationChannel channel,
        string recipient,
        string body,
        string subject)
    {
        var notification = Notification.Create(
            leadId,
            eventId,
            notificationType,
            channel,
            recipient,
            body,
            subject);

        Assert.That(notification.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(notification.LeadId, Is.EqualTo(leadId));
        Assert.That(notification.EventId, Is.EqualTo(eventId));
        Assert.That(notification.NotificationType, Is.EqualTo(notificationType));
        Assert.That(notification.Channel, Is.EqualTo(channel));
        Assert.That(notification.Recipient, Is.EqualTo(recipient));
        Assert.That(notification.Body, Is.EqualTo(body));
        Assert.That(notification.Subject, Is.EqualTo(subject));
        Assert.That(notification.Status, Is.EqualTo(NotificationStatus.Pending));
        Assert.That(notification.RetryCount, Is.EqualTo(0));
        Assert.That(notification.MaxRetries, Is.EqualTo(3));
        Assert.That(notification.CreatedAt, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
        Assert.That(notification.UpdatedAt, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
        Assert.That(notification.SentAt, Is.Null);
        Assert.That(notification.FailureReason, Is.Null);
        Assert.That(notification.NextRetryAt, Is.Null);
    }

    [Test, AutoData]
    public void Create_WithCustomMaxRetries_ShouldSetCorrectly(
        Guid leadId,
        string eventId,
        string notificationType,
        NotificationChannel channel,
        string recipient,
        string body,
        int maxRetries)
    {
        var notification = Notification.Create(
            leadId,
            eventId,
            notificationType,
            channel,
            recipient,
            body,
            maxRetries: maxRetries);

        Assert.That(notification.MaxRetries, Is.EqualTo(maxRetries));
    }

    #endregion

    #region MarkAsSent

    [Test, AutoData]
    public void MarkAsSent_WhenPending_ShouldUpdateStatusAndAddEvent(
        [WithValidNotification] Notification notification)
    {
        notification.MarkAsSent();

        Assert.That(notification.Status, Is.EqualTo(NotificationStatus.Sent));
        Assert.That(notification.SentAt, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
        Assert.That(notification.UpdatedAt, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
        Assert.That(notification.NextRetryAt, Is.Null);
        Assert.That(notification.DomainEvents, Has.Exactly(1).InstanceOf<NotificationSentDomainEvent>());

        var domainEvent = notification.DomainEvents.First() as NotificationSentDomainEvent;
        Assert.That(domainEvent!.LeadId, Is.EqualTo(notification.LeadId));
        Assert.That(domainEvent.NotificationType, Is.EqualTo(notification.NotificationType));
        Assert.That(domainEvent.Channel, Is.EqualTo(notification.Channel.ToString()));
        Assert.That(domainEvent.Status, Is.EqualTo(nameof(NotificationStatus.Sent)));
    }

    [Test, AutoData]
    public void MarkAsSent_WhenAlreadySent_ShouldThrow(
        [WithValidNotification] Notification notification)
    {
        notification.MarkAsSent();

        var ex = Assert.Throws<InvalidOperationException>(notification.MarkAsSent);

        Assert.That(ex.Message, Does.Contain("Cannot mark as sent"));
    }

    [Test, AutoData]
    public void MarkAsSent_WhenFailed_ShouldThrow(
        [WithValidNotification] Notification notification,
        string reason)
    {
        notification.MarkAsFailed(reason);

        var ex = Assert.Throws<InvalidOperationException>(notification.MarkAsSent);

        Assert.That(ex.Message, Does.Contain("Cannot mark as sent"));
    }

    #endregion

    #region MarkAsFailed

    [Test, AutoData]
    public void MarkAsFailed_WhenPending_ShouldUpdateStatusAndIncrementRetryCount(
        [WithValidNotification] Notification notification,
        string reason)
    {
        notification.MarkAsFailed(reason);

        Assert.That(notification.Status, Is.EqualTo(NotificationStatus.Failed));
        Assert.That(notification.FailureReason, Is.EqualTo(reason));
        Assert.That(notification.RetryCount, Is.EqualTo(1));
        Assert.That(notification.UpdatedAt, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
    }

    [Test, AutoData]
    public void MarkAsFailed_WhenRetryCountReachesMax_ShouldAddEvent(
        [WithValidNotification] Notification notification,
        string reason)
    {
        for (int i = 0; i < notification.MaxRetries; i++)
        {
            notification.MarkAsFailed(reason);
        }

        Assert.That(notification.DomainEvents, Has.Exactly(1).InstanceOf<NotificationSentDomainEvent>());

        var domainEvent = notification.DomainEvents.First() as NotificationSentDomainEvent;
        Assert.That(domainEvent!.Status, Does.Contain("Failed permanently"));
    }

    [Test, AutoData]
    public void MarkAsFailed_WhenRetryCountReachesMax_ShouldNotAddEventOnEachAttempt(
        [WithValidNotification] Notification notification,
        string reason)
    {
        notification.MarkAsFailed(reason);
        Assert.That(notification.DomainEvents, Is.Empty);

        notification.MarkAsFailed(reason);
        Assert.That(notification.DomainEvents, Is.Empty);

        notification.MarkAsFailed(reason);
        Assert.That(notification.DomainEvents, Has.Exactly(1).InstanceOf<NotificationSentDomainEvent>());
    }

    [Test, AutoData]
    public void MarkAsFailed_WithNextRetryAt_ShouldSetRetryTime(
        [WithValidNotification] Notification notification,
        string reason,
        DateTime nextRetryAt)
    {
        notification.MarkAsFailed(reason, nextRetryAt);

        Assert.That(notification.NextRetryAt, Is.EqualTo(nextRetryAt));
    }

    #endregion

    #region IsReadyForRetry

    [Test, AutoData]
    public void IsReadyForRetry_WhenFailedAndRetryCountLessThanMax_ShouldReturnTrue(
        [WithValidNotification] Notification notification,
        string reason)
    {
        notification.MarkAsFailed(reason);

        Assert.That(notification.RetryCount, Is.EqualTo(1));
        Assert.That(notification.MaxRetries, Is.EqualTo(3));
        Assert.That(notification.IsReadyForRetry(), Is.True);
    }

    [Test, AutoData]
    public void IsReadyForRetry_WhenFailedAndRetryCountEqualsMax_ShouldReturnFalse(
        [WithValidNotification] Notification notification,
        string reason)
    {
        for (int i = 0; i < notification.MaxRetries; i++)
        {
            notification.MarkAsFailed(reason);
        }

        Assert.That(notification.RetryCount, Is.EqualTo(notification.MaxRetries));
        Assert.That(notification.IsReadyForRetry(), Is.False);
    }

    [Test, AutoData]
    public void IsReadyForRetry_WhenFailedAndRetryCountGreaterThanMax_ShouldReturnFalse(
        [WithValidNotification] Notification notification,
        string reason)
    {
        for (int i = 0; i < notification.MaxRetries + 2; i++)
        {
            notification.MarkAsFailed(reason);
        }

        Assert.That(notification.RetryCount, Is.GreaterThan(notification.MaxRetries));
        Assert.That(notification.IsReadyForRetry(), Is.False);
    }

    [Test, AutoData]
    public void IsReadyForRetry_WhenPending_ShouldReturnFalse(
        [WithValidNotification] Notification notification)
    {
        Assert.That(notification.IsReadyForRetry(), Is.False);
    }

    [Test, AutoData]
    public void IsReadyForRetry_WhenSent_ShouldReturnFalse(
        [WithValidNotification] Notification notification)
    {
        notification.MarkAsSent();

        Assert.That(notification.IsReadyForRetry(), Is.False);
    }

    [Test, AutoData]
    public void IsReadyForRetry_WhenFailedWithFutureRetry_ShouldReturnFalse(
        [WithValidNotification] Notification notification,
        string reason)
    {
        var futureTime = DateTime.UtcNow.AddHours(1);
        notification.MarkAsFailed(reason, futureTime);

        Assert.That(notification.IsReadyForRetry(), Is.False);
    }

    [Test, AutoData]
    public void IsReadyForRetry_WhenFailedWithPastRetry_ShouldReturnTrue(
        [WithValidNotification] Notification notification,
        string reason)
    {
        var pastTime = DateTime.UtcNow.AddHours(-1);
        notification.MarkAsFailed(reason, pastTime);

        Assert.That(notification.RetryCount, Is.EqualTo(1));
        Assert.That(notification.IsReadyForRetry(), Is.True);
    }

    [Test, AutoData]
    public void IsReadyForRetry_WhenFailedMultipleTimesWithPastRetry_ShouldReturnTrue(
        [WithValidNotification] Notification notification,
        string reason)
    {
        notification.MarkAsFailed(reason, DateTime.UtcNow.AddHours(-2));

        notification = Notification.Create(
            notification.LeadId,
            notification.EventId,
            notification.NotificationType,
            notification.Channel,
            notification.Recipient,
            notification.Body,
            notification.Subject,
            notification.MaxRetries);
        notification.MarkAsFailed(reason, DateTime.UtcNow.AddHours(-1));

        Assert.That(notification.RetryCount, Is.EqualTo(1));
        Assert.That(notification.IsReadyForRetry(), Is.True);
    }

    #endregion

    #region ClearDomainEvents

    [Test, AutoData]
    public void ClearDomainEvents_ShouldClearEvents(
        [WithValidNotification] Notification notification)
    {
        notification.MarkAsSent();

        Assert.That(notification.DomainEvents, Is.Not.Empty);

        notification.ClearDomainEvents();

        Assert.That(notification.DomainEvents, Is.Empty);
    }

    #endregion

    #region State Transitions

    [Test, AutoData]
    public void MultipleMarkAsFailed_ShouldIncrementRetryCount(
        [WithValidNotification] Notification notification,
        string reason)
    {
        notification.MarkAsFailed(reason);
        Assert.That(notification.RetryCount, Is.EqualTo(1));
        Assert.That(notification.Status, Is.EqualTo(NotificationStatus.Failed));

        notification.MarkAsFailed(reason);
        Assert.That(notification.RetryCount, Is.EqualTo(2));
        Assert.That(notification.Status, Is.EqualTo(NotificationStatus.Failed));

        notification.MarkAsFailed(reason);
        Assert.That(notification.RetryCount, Is.EqualTo(3));
        Assert.That(notification.Status, Is.EqualTo(NotificationStatus.Failed));
    }

    [Test, AutoData]
    public void MarkAsFailed_ShouldUpdateUpdatedAt(
        [WithValidNotification] Notification notification,
        string reason)
    {
        var originalUpdatedAt = notification.UpdatedAt;

        Thread.Sleep(10);

        notification.MarkAsFailed(reason);

        Assert.That(notification.UpdatedAt, Is.GreaterThan(originalUpdatedAt));
    }

    #endregion
}