using AutoFixture.NUnit4;
using NUnit.Framework;
using ScoringService.Domain.Entities;

namespace ScoringService.Tests.Domain.Entities;

/// <summary>
/// Тесты для ScoringRule
/// </summary>
[Category("Domain")]
public class ScoringRuleTests
{
    [Test, AutoData]
    public void Create_WithValidData_ShouldCreateRule(
        Guid id,
        string ruleName,
        string conditionJson,
        int scoreValue,
        int priority)
    {
        var rule = ScoringRule.Create(id, ruleName, conditionJson, scoreValue, priority);

        Assert.That(rule.Id, Is.EqualTo(id));
        Assert.That(rule.RuleName, Is.EqualTo(ruleName));
        Assert.That(rule.ConditionJson, Is.EqualTo(conditionJson));
        Assert.That(rule.ScoreValue, Is.EqualTo(scoreValue));
        Assert.That(rule.Priority, Is.EqualTo(priority));
        Assert.That(rule.Description, Is.Empty);
        Assert.That(rule.IsActive, Is.True);
        Assert.That(rule.ValidFrom, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
        Assert.That(rule.ValidTo, Is.Null);
        Assert.That(rule.Version, Is.EqualTo(1));
    }

    [Test, AutoData]
    public void Create_WithDefaultPriority_ShouldSetPriorityToZero(
        Guid id,
        string ruleName,
        string conditionJson,
        int scoreValue)
    {
        var rule = ScoringRule.Create(id, ruleName, conditionJson, scoreValue);

        Assert.That(rule.Priority, Is.EqualTo(0));
    }
}