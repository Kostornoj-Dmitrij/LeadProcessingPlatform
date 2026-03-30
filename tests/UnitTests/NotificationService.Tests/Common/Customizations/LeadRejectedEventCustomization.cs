using AutoFixture;
using AvroSchemas.Messages.LeadEvents;

namespace NotificationService.Tests.Common.Customizations;

/// <summary>
/// Кастомизация для LeadRejected
/// </summary>
public class LeadRejectedEventCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<LeadRejected>(composer => composer
            .With(e => e.EventId, fixture.Create<Guid>())
            .With(e => e.LeadId, fixture.Create<Guid>())
            .With(e => e.Reason, "Lead validation failed")
            .With(e => e.ErrorDetails, "Email format is invalid")
            .With(e => e.FailureType, "ValidationFailed"));
    }
}