using AutoFixture;
using DistributionService.Domain.Entities;

namespace DistributionService.Tests.Common.Customizations;

/// <summary>
/// Кастомизация для успешной DistributionHistory
/// </summary>
public class DistributionHistorySuccessCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<DistributionHistory>(composer => composer
            .FromFactory(() =>
            {
                var leadId = fixture.Create<Guid>();
                var ruleId = fixture.Create<Guid>();
                var target = fixture.Create<string>().Substring(0, Math.Min(50, fixture.Create<string>().Length));
                return DistributionHistory.CreateSuccess(leadId, ruleId, target);
            }));
    }
}