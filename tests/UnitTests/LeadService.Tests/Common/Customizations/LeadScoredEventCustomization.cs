using AutoFixture;
using IntegrationEvents.ScoringEvents;

namespace LeadService.Tests.Common.Customizations;

/// <summary>
/// Кастомизация для LeadScoredIntegrationEvent
/// </summary>
public class LeadScoredEventCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<LeadScoredIntegrationEvent>(composer => composer
            .With(e => e.LeadId, fixture.Create<Guid>())
            .With(e => e.TotalScore, 75)
            .With(e => e.QualifiedThreshold, 50)
            .With(e => e.AppliedRules, ["RevenueRule", "IndustryRule"]));
    }
}