using System.Reflection;
using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.NUnit3;
using DistributionService.Tests.Common.Customizations;

namespace DistributionService.Tests.Common.Attributes;

/// <summary>
/// Атрибут для генерации валидного DistributionRequest
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class WithValidDistributionRequestAttribute : CustomizeAttribute
{
    public override ICustomization GetCustomization(ParameterInfo parameter)
    {
        return new CompositeCustomization(
            new AutoMoqCustomization(),
            new DistributionRequestCustomization());
    }
}