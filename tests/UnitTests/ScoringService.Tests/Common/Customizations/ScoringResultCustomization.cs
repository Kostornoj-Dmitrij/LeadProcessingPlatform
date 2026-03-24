using AutoFixture;
using ScoringService.Domain.Entities;

namespace ScoringService.Tests.Common.Customizations;

/// <summary>
/// Кастомизация для ScoringResult
/// </summary>
public class ScoringResultCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<ScoringResult>(composer => composer
            .FromFactory(() =>
            {
                var leadId = fixture.Create<Guid>();
                return ScoringResult.Create(leadId, 75, 50, ["base_rule", "industry_rule"]);
            }));
    }
}