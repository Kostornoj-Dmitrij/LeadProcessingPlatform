using AutoFixture;
using EnrichmentService.Domain.Entities;

namespace EnrichmentService.Tests.Common.Customizations;

/// <summary>
/// Кастомизация для EnrichmentRequest
/// </summary>
public class EnrichmentRequestCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<EnrichmentRequest>(composer => composer
            .FromFactory(() =>
            {
                var leadId = fixture.Create<Guid>();
                var companyName = fixture.Create<string>();
                var email = $"{fixture.Create<string>().ToLower()}@test.com";
                var contactPerson = fixture.Create<string>();
                var customFields = new Dictionary<string, string>
                {
                    { "industry", "Technology" }
                };

                return EnrichmentRequest.Create(
                    leadId, companyName, email, contactPerson, customFields);
            }));
    }
}