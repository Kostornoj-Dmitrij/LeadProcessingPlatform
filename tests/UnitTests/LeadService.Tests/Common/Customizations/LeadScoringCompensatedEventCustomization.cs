using AutoFixture;
using AvroSchemas.Messages.ScoringEvents;

namespace LeadService.Tests.Common.Customizations;

/// <summary>
/// Кастомизация для LeadScoringCompensatedEvent
/// </summary>
public class LeadScoringCompensatedEventCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<LeadScoringCompensated>(composer => composer
            .With(e => e.EventId, fixture.Create<Guid>())
            .With(e => e.LeadId, fixture.Create<Guid>())
            .With(e => e.Compensated, true));
    }
}