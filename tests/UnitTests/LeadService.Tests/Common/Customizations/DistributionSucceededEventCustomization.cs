using AutoFixture;
using IntegrationEvents.DistributionEvents;

namespace LeadService.Tests.Common.Customizations;

/// <summary>
/// Кастомизация для DistributionSucceededIntegrationEvent
/// </summary>
public class DistributionSucceededEventCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<DistributionSucceededIntegrationEvent>(composer => composer
            .With(e => e.LeadId, fixture.Create<Guid>())
            .With(e => e.Target, "sales_team"));
    }
}