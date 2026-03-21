using AutoFixture;
using IntegrationEvents.LeadEvents;

namespace EnrichmentService.Tests.Common.Customizations;

/// <summary>
/// Кастомизация для LeadRejectedIntegrationEvent
/// </summary>
public class LeadRejectedEventCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<LeadRejectedIntegrationEvent>(composer => composer
            .With(e => e.LeadId, fixture.Create<Guid>())
            .With(e => e.Reason, "Lead validation failed")
            .With(e => e.FailureType, "ValidationFailed"));
    }
}