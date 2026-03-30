using AutoFixture;
using AvroSchemas.Messages.LeadEvents;

namespace NotificationService.Tests.Common.Customizations;

/// <summary>
/// Кастомизация для LeadDistributionFailed
/// </summary>
public class LeadDistributionFailedEventCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<LeadDistributionFailed>(composer => composer
            .With(e => e.EventId, fixture.Create<Guid>())
            .With(e => e.LeadId, fixture.Create<Guid>())
            .With(e => e.Reason, "Distribution service unavailable"));
    }
}