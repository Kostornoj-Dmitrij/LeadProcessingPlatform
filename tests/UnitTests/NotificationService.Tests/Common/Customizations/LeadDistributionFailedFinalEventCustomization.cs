using AutoFixture;
using AvroSchemas.Messages.LeadEvents;

namespace NotificationService.Tests.Common.Customizations;

/// <summary>
/// Кастомизация для LeadDistributionFailedFinal
/// </summary>
public class LeadDistributionFailedFinalEventCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<LeadDistributionFailedFinal>(composer => composer
            .With(e => e.EventId, fixture.Create<Guid>())
            .With(e => e.LeadId, fixture.Create<Guid>())
            .With(e => e.FinalStatus, "Closed"));
    }
}