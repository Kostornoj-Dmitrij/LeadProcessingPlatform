using AutoFixture;
using AvroSchemas.Messages.EnrichmentEvents;

namespace LeadService.Tests.Common.Customizations;

/// <summary>
/// Кастомизация для LeadEnrichmentCompensatedEvent
/// </summary>
public class LeadEnrichmentCompensatedEventCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<LeadEnrichmentCompensated>(composer => composer
            .With(e => e.EventId, fixture.Create<Guid>())
            .With(e => e.LeadId, fixture.Create<Guid>())
            .With(e => e.Compensated, true));
    }
}