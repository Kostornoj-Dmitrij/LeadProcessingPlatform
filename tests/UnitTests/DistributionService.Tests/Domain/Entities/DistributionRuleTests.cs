using AutoFixture.NUnit4;
using DistributionService.Domain.Entities;
using DistributionService.Domain.Enums;
using NUnit.Framework;

namespace DistributionService.Tests.Domain.Entities;

/// <summary>
/// Тесты для DistributionRule
/// </summary>
[Category("Domain")]
public class DistributionRuleTests
{
    [Test, AutoData]
    public void Create_WithValidData_ShouldCreateRule(
        Guid id,
        string ruleName,
        DistributionRuleStrategy strategy,
        string conditionJson,
        string targetConfigJson,
        int priority)
    {
        var rule = DistributionRule.Create(id, ruleName, strategy, conditionJson, targetConfigJson, priority);

        Assert.That(rule.Id, Is.EqualTo(id));
        Assert.That(rule.RuleName, Is.EqualTo(ruleName));
        Assert.That(rule.Strategy, Is.EqualTo(strategy));
        Assert.That(rule.ConditionJson, Is.EqualTo(conditionJson));
        Assert.That(rule.TargetConfigJson, Is.EqualTo(targetConfigJson));
        Assert.That(rule.Priority, Is.EqualTo(priority));
        Assert.That(rule.IsActive, Is.True);
        Assert.That(rule.ValidFrom, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
        Assert.That(rule.ValidTo, Is.Null);
        Assert.That(rule.Version, Is.EqualTo(1));
    }

    [Test, AutoData]
    public void Create_WithDefaultPriority_ShouldSetPriorityToZero(
        Guid id,
        string ruleName,
        DistributionRuleStrategy strategy,
        string conditionJson,
        string targetConfigJson)
    {
        var rule = DistributionRule.Create(id, ruleName, strategy, conditionJson, targetConfigJson);

        Assert.That(rule.Priority, Is.EqualTo(0));
    }

    [Test, AutoData]
    public void Deactivate_ShouldSetIsActiveFalseAndValidToNow(
        DistributionRule rule)
    {
        rule.Deactivate();

        Assert.That(rule.IsActive, Is.False);
        Assert.That(rule.ValidTo, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
    }

    [Test, AutoData]
    public void UpdatePriority_ShouldChangePriority(
        DistributionRule rule,
        int newPriority)
    {
        rule.UpdatePriority(newPriority);

        Assert.That(rule.Priority, Is.EqualTo(newPriority));
    }

    [Test, AutoData]
    public void IsApplicable_WhenActiveAndValidFromInPastAndValidToNull_ShouldReturnTrue(
        DistributionRule rule)
    {
        Assert.That(rule.IsApplicable(DateTime.UtcNow), Is.True);
    }

    [Test, AutoData]
    public void IsApplicable_WhenInactive_ShouldReturnFalse(
        DistributionRule rule)
    {
        rule.Deactivate();

        Assert.That(rule.IsApplicable(DateTime.UtcNow), Is.False);
    }

    [Test, AutoData]
    public void IsApplicable_WhenValidFromInFuture_ShouldReturnFalse(
        DistributionRule rule)
    {
        var futureTime = rule.ValidFrom.AddSeconds(-1);

        Assert.That(rule.IsApplicable(futureTime), Is.False);
    }

    [Test, AutoData]
    public void IsApplicable_WhenValidToInPast_ShouldReturnFalse(
        DistributionRule rule)
    {
        rule.Deactivate();
        var pastTime = rule.ValidTo!.Value.AddSeconds(1);

        Assert.That(rule.IsApplicable(pastTime), Is.False);
    }

    [Test, AutoData]
    public void IsApplicable_WhenValidFromInPastAndValidToInFuture_ShouldReturnTrue(
        DistributionRule rule)
    {
        var currentTime = rule.ValidFrom.AddSeconds(1);

        Assert.That(rule.IsApplicable(currentTime), Is.True);
    }
}