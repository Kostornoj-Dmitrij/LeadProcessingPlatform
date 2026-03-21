using AutoFixture;
using EnrichmentService.Domain.Entities;

namespace EnrichmentService.Tests.Common.Customizations;

/// <summary>
/// Кастомизация для EnrichmentResult
/// </summary>
public class EnrichmentResultCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<EnrichmentResult>(composer => composer
            .FromFactory(() =>
            {
                var leadId = fixture.Create<Guid>();
                var companyName = fixture.Create<string>();

                return EnrichmentResult.Create(
                    leadId,
                    companyName,
                    "Technology",
                    "101-500",
                    "https://example.com",
                    "$10M-$50M",
                    "{\"raw\":\"data\"}");
            }));
    }
}