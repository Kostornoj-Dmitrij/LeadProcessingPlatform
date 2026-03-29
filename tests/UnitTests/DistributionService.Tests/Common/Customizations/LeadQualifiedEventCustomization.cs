using AutoFixture;
using AvroSchemas.Messages.LeadEvents;

namespace DistributionService.Tests.Common.Customizations;

/// <summary>
/// Кастомизация для LeadQualified
/// </summary>
public class LeadQualifiedEventCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<LeadQualified>(composer => composer
            .With(e => e.EventId, fixture.Create<Guid>())
            .With(e => e.LeadId, fixture.Create<Guid>())
            .With(e => e.CompanyName, fixture.Create<string>().Substring(0, Math.Min(50, fixture.Create<string>().Length)))
            .With(e => e.Email, () => $"{fixture.Create<string>().ToLower()}@test.com")
            .With(e => e.Score, 85)
            .With(e => e.ContactPerson, fixture.Create<string>())
            .With(e => e.EnrichedData, new EnrichedData
            {
                Industry = "Technology",
                CompanySize = "50-100",
                Website = "https://example.com",
                RevenueRange = "$10M-$50M",
                Version = 1
            })
            .With(e => e.CustomFields, new Dictionary<string, string>
            {
                { "source", "web_form" },
                { "campaign", "campaign" }
            }));
    }
}