using AutoFixture;
using EnrichmentService.Domain.Entities;

namespace EnrichmentService.Tests.Common.Customizations;

/// <summary>
/// Кастомизация для CompensationLog
/// </summary>
public class CompensationLogCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<CompensationLog>(composer => composer
            .FromFactory(() =>
            {
                var leadId = fixture.Create<Guid>();
                return CompensationLog.CreateEnrichmentCompensation(leadId);
            }));
    }
}