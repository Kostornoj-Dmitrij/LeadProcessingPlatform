using AutoFixture.NUnit3;
using EnrichmentService.Domain.Events;
using NUnit.Framework;

namespace EnrichmentService.Tests.Domain.Events;

/// <summary>
/// Тесты для доменных событий
/// </summary>
[Category("Domain")]
public class DomainEventTests
{
    [Test, AutoData]
    public void LeadEnrichedDomainEvent_ShouldSetProperties(
        Guid leadId,
        string industry,
        string companySize,
        string website,
        string revenueRange,
        int version)
    {
        var @event = new LeadEnrichedDomainEvent(
            leadId, industry, companySize, website, revenueRange, version);

        Assert.That(@event.LeadId, Is.EqualTo(leadId));
        Assert.That(@event.Industry, Is.EqualTo(industry));
        Assert.That(@event.CompanySize, Is.EqualTo(companySize));
        Assert.That(@event.Website, Is.EqualTo(website));
        Assert.That(@event.RevenueRange, Is.EqualTo(revenueRange));
        Assert.That(@event.Version, Is.EqualTo(version));
        Assert.That(@event.EventId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(@event.OccurredOn, Is.LessThanOrEqualTo(DateTime.UtcNow));
    }

    [Test, AutoData]
    public void LeadEnrichmentFailedDomainEvent_ShouldSetProperties(
        Guid leadId,
        string reason,
        int retryCount)
    {
        var @event = new LeadEnrichmentFailedDomainEvent(leadId, reason, retryCount);

        Assert.That(@event.LeadId, Is.EqualTo(leadId));
        Assert.That(@event.Reason, Is.EqualTo(reason));
        Assert.That(@event.RetryCount, Is.EqualTo(retryCount));
    }

    [Test, AutoData]
    public void LeadEnrichmentCompensatedDomainEvent_ShouldSetProperties(
        Guid leadId)
    {
        var @event = new LeadEnrichmentCompensatedDomainEvent(leadId);

        Assert.That(@event.LeadId, Is.EqualTo(leadId));
        Assert.That(@event.Compensated, Is.True);
    }
}