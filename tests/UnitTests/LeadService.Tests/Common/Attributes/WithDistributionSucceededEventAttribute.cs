using System.Reflection;
using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.NUnit3;
using LeadService.Tests.Common.Customizations;

namespace LeadService.Tests.Common.Attributes;

/// <summary>
/// Атрибут для генерации события DistributionSucceededIntegrationEvent
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class WithDistributionSucceededEventAttribute : CustomizeAttribute
{
    public override ICustomization GetCustomization(ParameterInfo parameter)
    {
        return new CompositeCustomization(
            new AutoMoqCustomization(),
            new DistributionSucceededEventCustomization());
    }
}