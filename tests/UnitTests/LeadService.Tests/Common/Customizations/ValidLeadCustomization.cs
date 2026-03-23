using AutoFixture;
using LeadService.Domain.Entities;
using LeadService.Domain.Enums;

namespace LeadService.Tests.Common.Customizations;

/// <summary>
/// Кастомизация валидного лида
/// </summary>
public class ValidLeadCustomization(LeadStatus status = LeadStatus.Initial) : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        fixture.Register(() =>
        {
            var str = Guid.NewGuid().ToString();
            return str.Length > 30 ? str.Substring(0, 30) : str;
        });

        fixture.Customize<LeadCustomField>(composer => composer
            .FromFactory(() =>
            {
                var fieldName = Guid.NewGuid().ToString().Substring(0, 20);
                var fieldValue = Guid.NewGuid().ToString();
                return new LeadCustomField(fieldName, fieldValue);
            }));

        fixture.Customize<Lead>(composer => composer
            .FromFactory(() =>
            {
                var id = fixture.Create<Guid>();
                var source = fixture.Create<string>();
                var companyName = fixture.Create<string>();
                var email = $"{Guid.NewGuid():N}@test.com";
                var externalLeadId = fixture.Create<string>();
                var contactPerson = fixture.Create<string>();
                var phone = $"+7{new Random().Next(1000000000, 1999999999)}";

                var customFields = new Dictionary<string, string>
                {
                    { "field1", Guid.NewGuid().ToString() },
                    { "field2", Guid.NewGuid().ToString() }
                };

                var lead = Lead.Create(
                    id: id,
                    source: source,
                    companyName: companyName,
                    email: email,
                    externalLeadId: externalLeadId,
                    contactPerson: contactPerson,
                    phone: phone,
                    customFields: customFields);

                var leadType = typeof(Lead);

                switch (status)
                {
                    case LeadStatus.Qualified:
                        leadType.GetProperty(nameof(Lead.IsEnrichmentReceived))?.SetValue(lead, true);
                        leadType.GetProperty(nameof(Lead.IsScoringReceived))?.SetValue(lead, true);
                        leadType.GetProperty(nameof(Lead.Score))?.SetValue(lead, 75);
                        leadType.GetProperty(nameof(Lead.Status))?.SetValue(lead, LeadStatus.Qualified);
                        break;
                    case LeadStatus.Rejected:
                        leadType.GetProperty(nameof(Lead.Status))?.SetValue(lead, LeadStatus.Rejected);
                        break;
                    case LeadStatus.Distributed:
                        leadType.GetProperty(nameof(Lead.IsEnrichmentReceived))?.SetValue(lead, true);
                        leadType.GetProperty(nameof(Lead.IsScoringReceived))?.SetValue(lead, true);
                        leadType.GetProperty(nameof(Lead.Score))?.SetValue(lead, 75);
                        leadType.GetProperty(nameof(Lead.Status))?.SetValue(lead, LeadStatus.Distributed);
                        break;
                    case LeadStatus.FailedDistribution:
                        leadType.GetProperty(nameof(Lead.IsEnrichmentReceived))?.SetValue(lead, true);
                        leadType.GetProperty(nameof(Lead.IsScoringReceived))?.SetValue(lead, true);
                        leadType.GetProperty(nameof(Lead.Score))?.SetValue(lead, 75);
                        leadType.GetProperty(nameof(Lead.Status))?.SetValue(lead, LeadStatus.FailedDistribution);
                        break;
                    case LeadStatus.Closed:
                        leadType.GetProperty(nameof(Lead.IsEnrichmentCompensated))?.SetValue(lead, true);
                        leadType.GetProperty(nameof(Lead.IsScoringCompensated))?.SetValue(lead, true);
                        leadType.GetProperty(nameof(Lead.Status))?.SetValue(lead, LeadStatus.Closed);
                        break;
                }

                return lead;
            }));
    }
}