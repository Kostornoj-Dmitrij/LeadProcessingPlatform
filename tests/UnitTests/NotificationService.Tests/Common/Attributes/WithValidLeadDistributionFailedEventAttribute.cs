using System.Reflection;
using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.NUnit4;
using NotificationService.Tests.Common.Customizations;

namespace NotificationService.Tests.Common.Attributes;

/// <summary>
/// Атрибут для генерации валидного события LeadDistributionFailed
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class WithValidLeadDistributionFailedEventAttribute : CustomizeAttribute
{
    public override ICustomization GetCustomization(ParameterInfo parameter)
    {
        return new CompositeCustomization(
            new AutoMoqCustomization(),
            new LeadDistributionFailedEventCustomization());
    }
}