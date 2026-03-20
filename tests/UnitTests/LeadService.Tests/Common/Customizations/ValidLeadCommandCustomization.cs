using AutoFixture;
using LeadService.Application.Commands.CreateLead;

namespace LeadService.Tests.Common.Customizations;

/// <summary>
/// Кастомизация валидной команды CrateLeadCommand
/// </summary>
public class ValidLeadCommandCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<CreateLeadCommand>(composer => composer
            .With(c => c.Email, 
                () => $"{fixture.Create<string>().ToLower()}@test.com")
            .With(c => c.Source, 
                () => fixture.Create<string>().Substring(0, Math.Min(50, fixture.Create<string>().Length)))
            .With(c => c.CompanyName, 
                () => fixture.Create<string>().Substring(0, Math.Min(50, fixture.Create<string>().Length)))
            .With(c => c.Phone, 
                () => $"+7{fixture.Create<int>() % 1000000000:D10}")
            .With(c => c.CustomFields, 
                () => new Dictionary<string, string>
                {
                    { "field1", fixture.Create<string>() },
                    { "field2", fixture.Create<string>() }
                }));
    }
}