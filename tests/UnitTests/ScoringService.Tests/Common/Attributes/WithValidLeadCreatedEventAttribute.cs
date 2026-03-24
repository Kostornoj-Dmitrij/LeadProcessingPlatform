using System.Reflection;
using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.NUnit3;
using ScoringService.Tests.Common.Customizations;

namespace ScoringService.Tests.Common.Attributes;

/// <summary>
/// Атрибут для генерации валидного события LeadCreatedIntegrationEvent
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class WithValidLeadCreatedEventAttribute : CustomizeAttribute
{
    public override ICustomization GetCustomization(ParameterInfo parameter)
    {
        return new CompositeCustomization(
            new AutoMoqCustomization(),
            new LeadCreatedEventCustomization());
    }
}