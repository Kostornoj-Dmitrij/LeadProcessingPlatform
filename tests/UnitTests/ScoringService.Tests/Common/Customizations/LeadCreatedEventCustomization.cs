using AutoFixture;
using AvroSchemas.Messages.LeadEvents;

namespace ScoringService.Tests.Common.Customizations;

/// <summary>
/// Кастомизация для LeadCreatedEvent
/// </summary>
public class LeadCreatedEventCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<LeadCreated>(composer => composer
            .With(e => e.EventId, fixture.Create<Guid>())
            .With(e => e.LeadId, fixture.Create<Guid>())
            .With(e => e.Source, "web_form")
            .With(e => e.CompanyName, fixture.Create<string>().Substring(0, Math.Min(50, fixture.Create<string>().Length)))
            .With(e => e.Email, () => $"{fixture.Create<string>().ToLower()}@test.com")
            .With(e => e.ContactPerson, fixture.Create<string>())
            .With(e => e.CustomFields, new Dictionary<string, string>
            {
                { "industry", "Technology" }
            }));
    }
}