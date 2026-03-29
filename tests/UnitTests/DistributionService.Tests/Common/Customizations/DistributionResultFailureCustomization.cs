using AutoFixture;
using DistributionService.Application.Common.DTOs;

namespace DistributionService.Tests.Common.Customizations;

/// <summary>
/// Кастомизация для неуспешного DistributionResult
/// </summary>
public class DistributionResultFailureCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<DistributionResult>(composer => composer
            .FromFactory(() => new DistributionResult(false, null, "Target system unavailable")));
    }
}