using System.Reflection;
using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.NUnit4;
using LeadService.Tests.Common.Customizations;

namespace LeadService.Tests.Common.Attributes;

/// <summary>
/// Атрибут для генерации события LeadScoringCompensatedEvent
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class WithLeadScoringCompensatedEventAttribute : CustomizeAttribute
{
    public override ICustomization GetCustomization(ParameterInfo parameter)
    {
        return new CompositeCustomization(
            new AutoMoqCustomization(),
            new LeadScoringCompensatedEventCustomization());
    }
}