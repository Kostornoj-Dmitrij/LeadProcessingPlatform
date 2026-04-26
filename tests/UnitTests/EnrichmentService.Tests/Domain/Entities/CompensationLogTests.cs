using AutoFixture.NUnit4;
using EnrichmentService.Domain.Constants;
using EnrichmentService.Domain.Entities;
using EnrichmentService.Domain.Events;
using EnrichmentService.Tests.Common.Attributes;
using NUnit.Framework;

namespace EnrichmentService.Tests.Domain.Entities;

/// <summary>
/// Тесты для CompensationLog
/// </summary>
[Category("Domain")]
public class CompensationLogTests
{
    [Test, AutoData]
    public void Create_WithValidData_ShouldCreateLog(
        Guid leadId,
        string reason)
    {
        var log = CompensationLog.CreateEnrichmentCompensation(leadId, reason);

        Assert.That(log.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(log.LeadId, Is.EqualTo(leadId));
        Assert.That(log.CompensationType, Is.EqualTo(CompensationConstants.EnrichmentCompensated));
        Assert.That(log.Reason, Is.EqualTo(reason));
        Assert.That(log.CreatedAt, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
        Assert.That(log.ProcessedAt, Is.Null);
        Assert.That(log.IsCompensated, Is.False);
        Assert.That(log.DomainEvents, Is.Empty);
    }

    [Test, AutoData]
    public void Create_WithoutReason_ShouldSetReasonToNull(
        Guid leadId)
    {
        var log = CompensationLog.CreateEnrichmentCompensation(leadId);

        Assert.That(log.Reason, Is.Null);
    }

    [Test, AutoData]
    public void MarkCompensated_ShouldMarkAndAddEvent(
        [WithValidCompensationLog] CompensationLog log)
    {
        log.MarkCompensated();

        Assert.That(log.IsCompensated, Is.True);
        Assert.That(log.ProcessedAt, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
        Assert.That(log.DomainEvents,
            Has.Exactly(1).InstanceOf<LeadEnrichmentCompensatedDomainEvent>());

        var domainEvent = log.DomainEvents.First() as LeadEnrichmentCompensatedDomainEvent;
        Assert.That(domainEvent!.LeadId, Is.EqualTo(log.LeadId));
        Assert.That(domainEvent.Compensated, Is.True);
    }

    [Test, AutoData]
    public void MarkCompensated_WhenCalledMultipleTimes_ShouldAddOnlyOneEvent(
        [WithValidCompensationLog] CompensationLog log)
    {
        log.MarkCompensated();
        log.ClearDomainEvents();

        log.MarkCompensated();

        Assert.That(log.IsCompensated, Is.True);
        Assert.That(log.DomainEvents,
            Has.Exactly(1).InstanceOf<LeadEnrichmentCompensatedDomainEvent>());
    }

    [Test, AutoData]
    public void ClearDomainEvents_ShouldClearEvents(
        [WithValidCompensationLog] CompensationLog log)
    {
        log.MarkCompensated();

        Assert.That(log.DomainEvents, Is.Not.Empty);

        log.ClearDomainEvents();

        Assert.That(log.DomainEvents, Is.Empty);
    }
}