using AutoFixture;
using ScoringService.Domain.Entities;

namespace ScoringService.Tests.Common.Customizations;

/// <summary>
/// Кастомизация для ScoringRequest
/// </summary>
public class ScoringRequestCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<ScoringRequest>(composer => composer
            .FromFactory(() =>
            {
                var leadId = fixture.Create<Guid>();
                var companyName = fixture.Create<string>();
                var email = $"{fixture.Create<string>().ToLower()}@test.com";
                var contactPerson = fixture.Create<string>();
                var customFields = new Dictionary<string, string> { { "industry", "Technology" } };

                return ScoringRequest.Create(leadId, companyName, email, contactPerson, customFields);
            }));
    }
}