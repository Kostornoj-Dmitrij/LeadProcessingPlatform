using System.Reflection;
using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.NUnit4;
using EnrichmentService.Tests.Common.Customizations;

namespace EnrichmentService.Tests.Common.Attributes;

/// <summary>
/// Атрибут для генерации валидного лога компенсации
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class WithValidCompensationLogAttribute : CustomizeAttribute
{
    public override ICustomization GetCustomization(ParameterInfo parameter)
    {
        return new CompositeCustomization(
            new AutoMoqCustomization(),
            new CompensationLogCustomization());
    }
}