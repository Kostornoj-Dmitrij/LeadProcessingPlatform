using AutoFixture.NUnit4;
using NUnit.Framework;
using AvroSchemas.Messages.NotificationEvents;
using NotificationService.Domain.Events;

namespace NotificationService.Tests.Domain.Events;

/// <summary>
/// Тесты для доменных событий
/// </summary>
[Category("Domain")]
public class DomainEventTests
{
    [Test, AutoData]
    public void NotificationSentDomainEvent_ShouldSetProperties(
        Guid leadId,
        string notificationType,
        string channel,
        string status)
    {
        var @event = new NotificationSentDomainEvent(leadId, notificationType, channel, status);

        Assert.That(@event.LeadId, Is.EqualTo(leadId));
        Assert.That(@event.NotificationType, Is.EqualTo(notificationType));
        Assert.That(@event.Channel, Is.EqualTo(channel));
        Assert.That(@event.Status, Is.EqualTo(status));
        Assert.That(@event.EventId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(@event.OccurredOn, Is.LessThanOrEqualTo(DateTime.UtcNow));
    }

    [Test, AutoData]
    public void NotificationSentDomainEvent_ToIntegrationEvent_ShouldMapCorrectly(
        Guid leadId,
        string notificationType,
        string channel,
        string status)
    {
        var domainEvent = new NotificationSentDomainEvent(leadId, notificationType, channel, status);
        var integrationEvent = domainEvent.ToIntegrationEvent();

        Assert.That(integrationEvent, Is.InstanceOf<NotificationSent>());
        var casted = integrationEvent as NotificationSent;
        Assert.That(casted!.LeadId, Is.EqualTo(leadId));
        Assert.That(casted.NotificationType, Is.EqualTo(notificationType));
        Assert.That(casted.Channel, Is.EqualTo(channel));
        Assert.That(casted.Status, Is.EqualTo(status));
        Assert.That(casted.EventId, Is.EqualTo(domainEvent.EventId));
        Assert.That(casted.SchemaVersion, Is.EqualTo(1));
    }
}