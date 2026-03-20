using AutoFixture;
using IntegrationEvents.ScoringEvents;

namespace LeadService.Tests.Common.Customizations;

/// <summary>
/// Кастомизация для LeadScoringCompensatedIntegrationEvent
/// </summary>
public class LeadScoringCompensatedEventCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<LeadScoringCompensatedIntegrationEvent>(composer => composer
            .With(e => e.LeadId, fixture.Create<Guid>())
            .With(e => e.Compensated, true));
    }
}