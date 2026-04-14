using AutoFixture;
using DistributionService.Domain.Entities;

namespace DistributionService.Tests.Common.Customizations;

/// <summary>
/// Кастомизация для DistributionRequest
/// </summary>
public class DistributionRequestCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<DistributionRequest>(composer => composer
            .FromFactory(() =>
            {
                var leadId = fixture.Create<Guid>();
                var companyName = fixture.Create<string>();
                companyName = companyName.Length > 50 ? companyName.Substring(0, 50) : companyName;
                var email = $"{fixture.Create<string>().ToLower()}@test.com";
                var score = fixture.Create<int>() % 100;
                var contactPerson = fixture.Create<string>();
                var customFields = new Dictionary<string, string>
                {
                    { "source", "web_form" },
                    { "campaign", "campaign" }
                };

                return DistributionRequest.Create(
                    leadId,
                    companyName,
                    email,
                    score,
                    contactPerson,
                    customFields);
            }));
    }
}