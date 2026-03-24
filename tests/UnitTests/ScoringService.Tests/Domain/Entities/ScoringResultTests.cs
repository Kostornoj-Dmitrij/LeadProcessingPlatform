using AutoFixture.NUnit3;
using NUnit.Framework;
using ScoringService.Domain.Entities;
using System.Text.Json;

namespace ScoringService.Tests.Domain.Entities;

/// <summary>
/// Тесты для ScoringResult
/// </summary>
[Category("Domain")]
public class ScoringResultTests
{
    [Test, AutoData]
    public void Create_WithValidData_ShouldCreateResult(
        Guid leadId,
        int totalScore,
        int qualifiedThreshold,
        List<string> appliedRules)
    {
        var result = ScoringResult.Create(leadId, totalScore, qualifiedThreshold, appliedRules);

        Assert.That(result.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(result.LeadId, Is.EqualTo(leadId));
        Assert.That(result.TotalScore, Is.EqualTo(totalScore));
        Assert.That(result.QualifiedThreshold, Is.EqualTo(qualifiedThreshold));
        Assert.That(result.CalculatedAt, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));

        var deserializedRules = JsonSerializer.Deserialize<List<string>>(result.AppliedRulesJson);
        Assert.That(deserializedRules, Is.EquivalentTo(appliedRules));
    }
}