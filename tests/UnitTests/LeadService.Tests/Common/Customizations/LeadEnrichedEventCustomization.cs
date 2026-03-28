using AutoFixture;
using AvroSchemas.Messages.EnrichmentEvents;

namespace LeadService.Tests.Common.Customizations;

/// <summary>
/// Кастомизация для LeadEnrichedEvent
/// </summary>
public class LeadEnrichedEventCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<LeadEnriched>(composer => composer
            .With(e => e.EventId, fixture.Create<Guid>())
            .With(e => e.LeadId, fixture.Create<Guid>())
            .With(e => e.Industry, "Technology")
            .With(e => e.CompanySize, "50-100")
            .With(e => e.Website, "https://example.com")
            .With(e => e.Version, 1));
    }
}