using AutoFixture.NUnit3;
using DistributionService.Domain.Events;
using NUnit.Framework;

namespace DistributionService.Tests.Domain.Events;

/// <summary>
/// Тесты для доменных событий
/// </summary>
[Category("Domain")]
public class DomainEventTests
{
    [Test, AutoData]
    public void DistributionSucceededDomainEvent_ShouldSetProperties(
        Guid leadId,
        string target,
        DateTime distributedAt)
    {
        var @event = new DistributionSucceededDomainEvent(leadId, target, distributedAt);

        Assert.That(@event.LeadId, Is.EqualTo(leadId));
        Assert.That(@event.Target, Is.EqualTo(target));
        Assert.That(@event.DistributedAt, Is.EqualTo(distributedAt));
        Assert.That(@event.EventId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(@event.OccurredOn, Is.LessThanOrEqualTo(DateTime.UtcNow));
    }

    [Test, AutoData]
    public void DistributionSucceededDomainEvent_ToIntegrationEvent_ShouldMapCorrectly(
        Guid leadId,
        string target,
        DateTime distributedAt)
    {
        var @event = new DistributionSucceededDomainEvent(leadId, target, distributedAt);
        var integrationEvent = @event.ToIntegrationEvent();

        Assert.That(integrationEvent, Is.InstanceOf<AvroSchemas.Messages.DistributionEvents.DistributionSucceeded>());
        var casted = integrationEvent as AvroSchemas.Messages.DistributionEvents.DistributionSucceeded;
        Assert.That(casted!.LeadId, Is.EqualTo(leadId));
        Assert.That(casted.Target, Is.EqualTo(target));
        Assert.That(casted.DistributedAt, Is.EqualTo(new DateTimeOffset(distributedAt).ToUnixTimeMilliseconds()));
    }

    [Test, AutoData]
    public void DistributionFailedDomainEvent_ShouldSetProperties(
        Guid leadId,
        string reason)
    {
        var @event = new DistributionFailedDomainEvent(leadId, reason);

        Assert.That(@event.LeadId, Is.EqualTo(leadId));
        Assert.That(@event.Reason, Is.EqualTo(reason));
        Assert.That(@event.EventId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(@event.OccurredOn, Is.LessThanOrEqualTo(DateTime.UtcNow));
    }

    [Test, AutoData]
    public void DistributionFailedDomainEvent_ToIntegrationEvent_ShouldMapCorrectly(
        Guid leadId,
        string reason)
    {
        var @event = new DistributionFailedDomainEvent(leadId, reason);
        var integrationEvent = @event.ToIntegrationEvent();

        Assert.That(integrationEvent, Is.InstanceOf<AvroSchemas.Messages.DistributionEvents.DistributionFailed>());
        var casted = integrationEvent as AvroSchemas.Messages.DistributionEvents.DistributionFailed;
        Assert.That(casted!.LeadId, Is.EqualTo(leadId));
        Assert.That(casted.Reason, Is.EqualTo(reason));
    }
}