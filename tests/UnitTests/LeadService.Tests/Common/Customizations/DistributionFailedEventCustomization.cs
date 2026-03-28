using AutoFixture;
using AvroSchemas.Messages.DistributionEvents;

namespace LeadService.Tests.Common.Customizations;

/// <summary>
/// Кастомизация для DistributionFailedEvent
/// </summary>
public class DistributionFailedEventCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<DistributionFailed>(composer => composer
            .With(e => e.EventId, fixture.Create<Guid>())
            .With(e => e.LeadId, fixture.Create<Guid>())
            .With(e => e.Reason, "CRM unavailable"));
    }
}