using System.Reflection;
using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.NUnit3;
using DistributionService.Tests.Common.Customizations;

namespace DistributionService.Tests.Common.Attributes;

/// <summary>
/// Атрибут для генерации неуспешного результата распределения
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class WithDistributionResultFailureAttribute : CustomizeAttribute
{
    public override ICustomization GetCustomization(ParameterInfo parameter)
    {
        return new CompositeCustomization(
            new AutoMoqCustomization(),
            new DistributionResultFailureCustomization());
    }
}