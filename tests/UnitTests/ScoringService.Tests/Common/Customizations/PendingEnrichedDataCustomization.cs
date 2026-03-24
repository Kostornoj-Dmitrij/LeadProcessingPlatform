using AutoFixture;
using ScoringService.Domain.Entities;
using System.Text.Json;

namespace ScoringService.Tests.Common.Customizations;

/// <summary>
/// Кастомизация для PendingEnrichedData
/// </summary>
public class PendingEnrichedDataCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<PendingEnrichedData>(composer => composer
            .FromFactory(() =>
            {
                var leadId = fixture.Create<Guid>();
                var enrichedDataJson = JsonSerializer.Serialize(new
                {
                    Industry = "Technology",
                    CompanySize = "50-100",
                    Website = "https://example.com"
                });
                return PendingEnrichedData.Create(leadId, enrichedDataJson);
            }));
    }
}