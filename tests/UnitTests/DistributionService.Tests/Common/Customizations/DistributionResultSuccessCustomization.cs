using AutoFixture;
using DistributionService.Application.Common.DTOs;

namespace DistributionService.Tests.Common.Customizations;

/// <summary>
/// Кастомизация для успешного DistributionResult
/// </summary>
public class DistributionResultSuccessCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<DistributionResult>(composer => composer
            .FromFactory(() => new DistributionResult(true, "{\"status\":\"ok\",\"id\":\"123\"}", null)));
    }
}