using System.Reflection;
using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.NUnit3;
using DistributionService.Domain.Enums;
using DistributionService.Tests.Common.Customizations;

namespace DistributionService.Tests.Common.Attributes;

/// <summary>
/// Атрибут для генерации валидного правила распределения
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class WithValidDistributionRuleAttribute(
    DistributionRuleStrategy strategy = DistributionRuleStrategy.FixedTarget,
    string conditionJson = "{\"type\":\"always_true\"}") : CustomizeAttribute
{
    public override ICustomization GetCustomization(ParameterInfo parameter)
    {
        return new CompositeCustomization(
            new AutoMoqCustomization(),
            new DistributionRuleCustomization(strategy, conditionJson));
    }
}