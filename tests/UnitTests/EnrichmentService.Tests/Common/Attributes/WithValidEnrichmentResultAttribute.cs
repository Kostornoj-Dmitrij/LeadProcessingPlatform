using System.Reflection;
using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.NUnit3;
using EnrichmentService.Tests.Common.Customizations;

namespace EnrichmentService.Tests.Common.Attributes;

/// <summary>
/// Атрибут для генерации валидного результата обогащения
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class WithValidEnrichmentResultAttribute : CustomizeAttribute
{
    public override ICustomization GetCustomization(ParameterInfo parameter)
    {
        return new CompositeCustomization(
            new AutoMoqCustomization(),
            new EnrichmentResultCustomization());
    }
}