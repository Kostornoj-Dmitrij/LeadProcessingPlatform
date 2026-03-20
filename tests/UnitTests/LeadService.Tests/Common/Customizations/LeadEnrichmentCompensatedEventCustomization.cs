using AutoFixture;
using IntegrationEvents.EnrichmentEvents;

namespace LeadService.Tests.Common.Customizations;

/// <summary>
/// Кастомизация для LeadEnrichmentCompensatedIntegrationEvent
/// </summary>
public class LeadEnrichmentCompensatedEventCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<LeadEnrichmentCompensatedIntegrationEvent>(composer => composer
            .With(e => e.LeadId, fixture.Create<Guid>())
            .With(e => e.Compensated, true));
    }
}