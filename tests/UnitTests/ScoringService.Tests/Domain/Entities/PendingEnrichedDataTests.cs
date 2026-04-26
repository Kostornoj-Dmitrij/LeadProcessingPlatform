using AutoFixture.NUnit4;
using NUnit.Framework;
using ScoringService.Domain.Entities;

namespace ScoringService.Tests.Domain.Entities;

/// <summary>
/// Тесты для PendingEnrichedData
/// </summary>
[Category("Domain")]
public class PendingEnrichedDataTests
{
    [Test, AutoData]
    public void Create_WithValidData_ShouldCreatePendingData(
        Guid leadId,
        string enrichedDataJson)
    {
        var pendingData = PendingEnrichedData.Create(leadId, enrichedDataJson);

        Assert.That(pendingData.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(pendingData.LeadId, Is.EqualTo(leadId));
        Assert.That(pendingData.EnrichedDataJson, Is.EqualTo(enrichedDataJson));
        Assert.That(pendingData.ReceivedAt, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
        Assert.That(pendingData.IsProcessed, Is.False);
    }

    [Test, AutoData]
    public void MarkAsProcessed_ShouldSetFlag(
        PendingEnrichedData pendingData)
    {
        pendingData.MarkAsProcessed();

        Assert.That(pendingData.IsProcessed, Is.True);
    }
}