using AutoFixture;
using AvroSchemas.Messages.ScoringEvents;

namespace LeadService.Tests.Common.Customizations;

/// <summary>
/// Кастомизация для LeadScoringFailedEvent
/// </summary>
public class LeadScoringFailedEventCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<LeadScoringFailed>(composer => composer
            .With(e => e.EventId, fixture.Create<Guid>())
            .With(e => e.LeadId, fixture.Create<Guid>())
            .With(e => e.Reason, "Scoring service unavailable"));
    }
}