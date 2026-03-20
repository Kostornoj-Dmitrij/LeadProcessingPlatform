using AutoFixture;
using IntegrationEvents.EnrichmentEvents;

namespace LeadService.Tests.Common.Customizations;

/// <summary>
/// Кастомизация для LeadEnrichmentFailedIntegrationEvent
/// </summary>
public class LeadEnrichmentFailedEventCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<LeadEnrichmentFailedIntegrationEvent>(composer => composer
            .With(e => e.LeadId, fixture.Create<Guid>())
            .With(e => e.Reason, "External API timeout")
            .With(e => e.RetryCount, 3));
    }
}