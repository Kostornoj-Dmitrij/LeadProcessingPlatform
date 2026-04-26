using System.Reflection;
using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.NUnit4;
using ScoringService.Tests.Common.Customizations;

namespace ScoringService.Tests.Common.Attributes;

/// <summary>
/// Атрибут для генерации валидных отложенных обогащенных данных
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class WithValidPendingEnrichedDataAttribute : CustomizeAttribute
{
    public override ICustomization GetCustomization(ParameterInfo parameter)
    {
        return new CompositeCustomization(
            new AutoMoqCustomization(),
            new PendingEnrichedDataCustomization());
    }
}