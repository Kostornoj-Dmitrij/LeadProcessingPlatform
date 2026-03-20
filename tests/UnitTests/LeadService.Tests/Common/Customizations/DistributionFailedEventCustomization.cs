using AutoFixture;
using IntegrationEvents.DistributionEvents;

namespace LeadService.Tests.Common.Customizations;

/// <summary>
/// Кастомизация для DistributionFailedIntegrationEvent
/// </summary>
public class DistributionFailedEventCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<DistributionFailedIntegrationEvent>(composer => composer
            .With(e => e.LeadId, fixture.Create<Guid>())
            .With(e => e.Reason, "CRM unavailable"));
    }
}