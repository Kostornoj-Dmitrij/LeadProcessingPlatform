using AutoFixture;
using IntegrationEvents.ScoringEvents;

namespace LeadService.Tests.Common.Customizations;

/// <summary>
/// Кастомизация для LeadScoringFailedIntegrationEvent
/// </summary>
public class LeadScoringFailedEventCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<LeadScoringFailedIntegrationEvent>(composer => composer
            .With(e => e.LeadId, fixture.Create<Guid>())
            .With(e => e.Reason, "Scoring service unavailable"));
    }
}