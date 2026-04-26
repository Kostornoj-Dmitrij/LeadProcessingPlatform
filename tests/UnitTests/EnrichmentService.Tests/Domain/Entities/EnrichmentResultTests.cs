using AutoFixture.NUnit4;
using EnrichmentService.Domain.Entities;
using EnrichmentService.Domain.Events;
using EnrichmentService.Tests.Common.Attributes;
using NUnit.Framework;

namespace EnrichmentService.Tests.Domain.Entities;

/// <summary>
/// Тесты для EnrichmentResult
/// </summary>
[Category("Domain")]
public class EnrichmentResultTests
{
    #region Create

    [Test, AutoData]
    public void Create_WithValidData_ShouldCreateResult(
        Guid leadId,
        string companyName,
        string industry,
        string companySize,
        string website,
        string revenueRange,
        string rawResponse)
    {
        var result = EnrichmentResult.Create(
            leadId, companyName, industry, companySize, website, revenueRange, rawResponse);

        Assert.That(result.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(result.LeadId, Is.EqualTo(leadId));
        Assert.That(result.CompanyName, Is.EqualTo(companyName));
        Assert.That(result.Industry, Is.EqualTo(industry));
        Assert.That(result.CompanySize, Is.EqualTo(companySize));
        Assert.That(result.Website, Is.EqualTo(website));
        Assert.That(result.RevenueRange, Is.EqualTo(revenueRange));
        Assert.That(result.RawResponse, Is.EqualTo(rawResponse));
        Assert.That(result.EnrichedAt, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
        Assert.That(result.DomainEvents, Has.Exactly(1).InstanceOf<LeadEnrichedDomainEvent>());

        var domainEvent = result.DomainEvents.First() as LeadEnrichedDomainEvent;
        Assert.That(domainEvent!.LeadId, Is.EqualTo(leadId));
        Assert.That(domainEvent.Industry, Is.EqualTo(industry));
        Assert.That(domainEvent.CompanySize, Is.EqualTo(companySize));
        Assert.That(domainEvent.Website, Is.EqualTo(website));
        Assert.That(domainEvent.RevenueRange, Is.EqualTo(revenueRange));
        Assert.That(domainEvent.Version, Is.EqualTo(1));
    }

    [Test, AutoData]
    public void Create_WithNullOptionalFields_ShouldSetToNull(
        Guid leadId,
        string companyName,
        string industry,
        string companySize)
    {
        var result = EnrichmentResult.Create(
            leadId, companyName, industry, companySize);

        Assert.That(result.Website, Is.Null);
        Assert.That(result.RevenueRange, Is.Null);
        Assert.That(result.RawResponse, Is.Null);
    }

    [Test, AutoData]
    public void Create_WithEmptyLeadId_ShouldThrow(
        string companyName,
        string industry,
        string companySize)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            EnrichmentResult.Create(Guid.Empty, companyName, industry, companySize));

        Assert.That(ex.Message, Does.Contain("LeadId cannot be empty"));
    }

    #endregion

    #region ClearDomainEvents

    [Test, AutoData]
    public void ClearDomainEvents_ShouldClearEvents(
        [WithValidEnrichmentResult] EnrichmentResult result)
    {
        Assert.That(result.DomainEvents, Is.Not.Empty);

        result.ClearDomainEvents();

        Assert.That(result.DomainEvents, Is.Empty);
    }

    #endregion
}