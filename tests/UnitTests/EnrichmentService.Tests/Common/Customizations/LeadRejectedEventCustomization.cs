using AutoFixture;
using AvroSchemas.Messages.LeadEvents;

namespace EnrichmentService.Tests.Common.Customizations;

/// <summary>
/// Кастомизация для LeadRejectedEvent
/// </summary>
public class LeadRejectedEventCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<LeadRejected>(composer => composer
            .With(e => e.EventId, fixture.Create<Guid>())
            .With(e => e.LeadId, fixture.Create<Guid>())
            .With(e => e.Reason, "Lead validation failed")
            .With(e => e.FailureType, "ValidationFailed"));
    }
}