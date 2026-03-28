using AutoFixture;
using AvroSchemas.Messages.EnrichmentEvents;

namespace LeadService.Tests.Common.Customizations;

/// <summary>
/// Кастомизация для LeadEnrichmentFailedEvent
/// </summary>
public class LeadEnrichmentFailedEventCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<LeadEnrichmentFailed>(composer => composer
            .With(e => e.EventId, fixture.Create<Guid>())
            .With(e => e.LeadId, fixture.Create<Guid>())
            .With(e => e.Reason, "External API timeout")
            .With(e => e.RetryCount, 3));
    }
}