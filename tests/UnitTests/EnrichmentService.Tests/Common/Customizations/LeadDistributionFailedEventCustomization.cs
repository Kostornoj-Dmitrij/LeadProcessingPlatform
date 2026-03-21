using AutoFixture;
using IntegrationEvents.LeadEvents;

namespace EnrichmentService.Tests.Common.Customizations;

/// <summary>
/// Кастомизация для LeadDistributionFailedIntegrationEvent
/// </summary>
public class LeadDistributionFailedEventCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<LeadDistributionFailedIntegrationEvent>(composer => composer
            .With(e => e.LeadId, fixture.Create<Guid>())
            .With(e => e.Reason, "Distribution service unavailable"));
    }
}