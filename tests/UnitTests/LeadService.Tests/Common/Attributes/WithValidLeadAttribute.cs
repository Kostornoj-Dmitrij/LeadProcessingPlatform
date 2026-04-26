using System.Reflection;
using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.NUnit4;
using LeadService.Domain.Enums;
using LeadService.Tests.Common.Customizations;

namespace LeadService.Tests.Common.Attributes;

/// <summary>
/// Атрибут для генерации валидного лида
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class WithValidLeadAttribute(LeadStatus status = LeadStatus.Initial) : CustomizeAttribute
{
    public override ICustomization GetCustomization(ParameterInfo parameter)
    {
        return new CompositeCustomization(
            new AutoMoqCustomization(),
            new ValidLeadCustomization(status));
    }
}