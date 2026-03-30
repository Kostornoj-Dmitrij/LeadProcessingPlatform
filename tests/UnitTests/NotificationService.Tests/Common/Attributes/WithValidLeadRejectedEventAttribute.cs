using System.Reflection;
using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.NUnit3;
using NotificationService.Tests.Common.Customizations;

namespace NotificationService.Tests.Common.Attributes;

/// <summary>
/// Атрибут для генерации валидного события LeadRejected
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class WithValidLeadRejectedEventAttribute : CustomizeAttribute
{
    public override ICustomization GetCustomization(ParameterInfo parameter)
    {
        return new CompositeCustomization(
            new AutoMoqCustomization(),
            new LeadRejectedEventCustomization());
    }
}