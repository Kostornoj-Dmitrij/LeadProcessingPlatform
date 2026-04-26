using System.Reflection;
using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.NUnit4;
using DistributionService.Tests.Common.Customizations;

namespace DistributionService.Tests.Common.Attributes;

/// <summary>
/// Атрибут для генерации валидного события LeadQualified
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class WithValidLeadQualifiedEventAttribute : CustomizeAttribute
{
    public override ICustomization GetCustomization(ParameterInfo parameter)
    {
        return new CompositeCustomization(
            new AutoMoqCustomization(),
            new LeadQualifiedEventCustomization());
    }
}