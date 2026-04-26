using AutoFixture.NUnit4;
using NUnit.Framework;
using ScoringService.Domain.Events;

namespace ScoringService.Tests.Domain.Events;

/// <summary>
/// Тесты для доменных событий
/// </summary>
[Category("Domain")]
public class DomainEventTests
{
    [Test, AutoData]
    public void LeadScoredDomainEvent_ShouldSetProperties(
        Guid leadId,
        int totalScore,
        int qualifiedThreshold,
        List<string> appliedRules)
    {
        var @event = new LeadScoredDomainEvent(leadId, totalScore, qualifiedThreshold, appliedRules);

        Assert.That(@event.LeadId, Is.EqualTo(leadId));
        Assert.That(@event.TotalScore, Is.EqualTo(totalScore));
        Assert.That(@event.QualifiedThreshold, Is.EqualTo(qualifiedThreshold));
        Assert.That(@event.AppliedRules, Is.EqualTo(appliedRules));
        Assert.That(@event.EventId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(@event.OccurredOn, Is.LessThanOrEqualTo(DateTime.UtcNow));
    }

    [Test, AutoData]
    public void LeadScoringFailedDomainEvent_ShouldSetProperties(
        Guid leadId,
        string reason,
        int retryCount)
    {
        var @event = new LeadScoringFailedDomainEvent(leadId, reason, retryCount);

        Assert.That(@event.LeadId, Is.EqualTo(leadId));
        Assert.That(@event.Reason, Is.EqualTo(reason));
        Assert.That(@event.RetryCount, Is.EqualTo(retryCount));
    }

    [Test, AutoData]
    public void LeadScoringCompensatedDomainEvent_ShouldSetProperties(
        Guid leadId)
    {
        var @event = new LeadScoringCompensatedDomainEvent(leadId);

        Assert.That(@event.LeadId, Is.EqualTo(leadId));
        Assert.That(@event.Compensated, Is.True);
    }
}