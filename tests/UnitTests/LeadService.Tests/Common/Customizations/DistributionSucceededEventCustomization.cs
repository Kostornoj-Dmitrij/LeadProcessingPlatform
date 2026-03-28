using AutoFixture;
using AvroSchemas.Messages.DistributionEvents;

namespace LeadService.Tests.Common.Customizations;

/// <summary>
/// Кастомизация для DistributionSucceededEvent
/// </summary>
public class DistributionSucceededEventCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<DistributionSucceeded>(composer => composer
            .With(e => e.EventId, fixture.Create<Guid>())
            .With(e => e.LeadId, fixture.Create<Guid>())
            .With(e => e.Target, "sales_team"));
    }
}