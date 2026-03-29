using AutoFixture;
using DistributionService.Domain.Entities;
using DistributionService.Domain.Enums;

namespace DistributionService.Tests.Common.Customizations;

/// <summary>
/// Кастомизация для DistributionRule
/// </summary>
public class DistributionRuleCustomization(DistributionRuleStrategy strategy, string conditionJson) : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<DistributionRule>(composer => composer
            .FromFactory(() =>
            {
                var id = fixture.Create<Guid>();
                var ruleName = fixture.Create<string>().Substring(0, Math.Min(50, fixture.Create<string>().Length));
                var targetConfigJson = strategy switch
                {
                    DistributionRuleStrategy.FixedTarget => "{\"target\":\"crm_system\"}",
                    DistributionRuleStrategy.Territory => "{\"territories\":{\"technology\":\"tech_team\",\"default\":\"general_team\"}}",
                    DistributionRuleStrategy.Specialization => "{\"specializations\":{\"50-100\":\"mid_market\",\"default\":\"general\"}}",
                    DistributionRuleStrategy.ScoreBased => "{\"thresholds\":[{\"min_score\":80,\"target\":\"premium\"}],\"default_target\":\"standard\"}",
                    DistributionRuleStrategy.RoundRobin => "{\"targets\":[\"rep1\",\"rep2\"]}",
                    _ => "{}"
                };

                return DistributionRule.Create(
                    id,
                    ruleName,
                    strategy,
                    conditionJson,
                    targetConfigJson,
                    fixture.Create<int>() % 100);
            }));
    }
}