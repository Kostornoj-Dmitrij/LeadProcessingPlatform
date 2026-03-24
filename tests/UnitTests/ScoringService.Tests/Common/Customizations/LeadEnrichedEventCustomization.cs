using AutoFixture;
using IntegrationEvents.EnrichmentEvents;

namespace ScoringService.Tests.Common.Customizations;

/// <summary>
/// Кастомизация для LeadEnrichedIntegrationEvent
/// </summary>
public class LeadEnrichedEventCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<LeadEnrichedIntegrationEvent>(composer => composer
            .With(e => e.LeadId, fixture.Create<Guid>())
            .With(e => e.Industry, "Technology")
            .With(e => e.CompanySize, "50-100")
            .With(e => e.Website, "https://example.com")
            .With(e => e.RevenueRange, "$10M-$50M")
            .With(e => e.Version, 1));
    }
}