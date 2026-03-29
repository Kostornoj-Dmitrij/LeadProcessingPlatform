using AutoFixture.NUnit3;
using DistributionService.Domain.Entities;
using DistributionService.Domain.Enums;
using DistributionService.Domain.Events;
using DistributionService.Tests.Common.Attributes;
using NUnit.Framework;

namespace DistributionService.Tests.Domain.Entities;

/// <summary>
/// Тесты для DistributionHistory
/// </summary>
[Category("Domain")]
public class DistributionHistoryTests
{
    [Test, AutoData]
    public void CreateSuccess_WithValidData_ShouldCreateHistoryWithSuccessEvent(
        Guid leadId,
        Guid ruleId,
        string target,
        string responseData)
    {
        var history = DistributionHistory.CreateSuccess(leadId, ruleId, target, responseData);

        Assert.That(history.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(history.LeadId, Is.EqualTo(leadId));
        Assert.That(history.RuleId, Is.EqualTo(ruleId));
        Assert.That(history.Target, Is.EqualTo(target));
        Assert.That(history.Status, Is.EqualTo(DistributionStatus.Success));
        Assert.That(history.ResponseData, Is.EqualTo(responseData));
        Assert.That(history.ErrorMessage, Is.Null);
        Assert.That(history.DistributedAt, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
        Assert.That(history.DomainEvents, Has.Exactly(1).InstanceOf<DistributionSucceededDomainEvent>());

        var domainEvent = history.DomainEvents.First() as DistributionSucceededDomainEvent;
        Assert.That(domainEvent!.LeadId, Is.EqualTo(leadId));
        Assert.That(domainEvent.Target, Is.EqualTo(target));
    }

    [Test, AutoData]
    public void CreateSuccess_WithNullRuleId_ShouldCreateHistory(
        Guid leadId,
        string target)
    {
        var history = DistributionHistory.CreateSuccess(leadId, null, target);

        Assert.That(history.RuleId, Is.Null);
    }

    [Test, AutoData]
    public void CreateSuccess_WithNullResponseData_ShouldCreateHistory(
        Guid leadId,
        Guid ruleId,
        string target)
    {
        var history = DistributionHistory.CreateSuccess(leadId, ruleId, target);

        Assert.That(history.ResponseData, Is.Null);
    }

    [Test, AutoData]
    public void CreateFailed_WithValidData_ShouldCreateHistoryWithFailedEvent(
        Guid leadId,
        Guid ruleId,
        string errorMessage,
        string attemptedTarget)
    {
        var history = DistributionHistory.CreateFailed(leadId, ruleId, errorMessage, attemptedTarget);

        Assert.That(history.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(history.LeadId, Is.EqualTo(leadId));
        Assert.That(history.RuleId, Is.EqualTo(ruleId));
        Assert.That(history.Target, Is.EqualTo(attemptedTarget));
        Assert.That(history.Status, Is.EqualTo(DistributionStatus.Failed));
        Assert.That(history.ErrorMessage, Is.EqualTo(errorMessage));
        Assert.That(history.ResponseData, Is.Null);
        Assert.That(history.DistributedAt, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
        Assert.That(history.DomainEvents, Has.Exactly(1).InstanceOf<DistributionFailedDomainEvent>());

        var domainEvent = history.DomainEvents.First() as DistributionFailedDomainEvent;
        Assert.That(domainEvent!.LeadId, Is.EqualTo(leadId));
        Assert.That(domainEvent.Reason, Is.EqualTo(errorMessage));
    }

    [Test, AutoData]
    public void CreateFailed_WithoutAttemptedTarget_ShouldSetEmptyTarget(
        Guid leadId,
        string errorMessage)
    {
        var history = DistributionHistory.CreateFailed(leadId, null, errorMessage);

        Assert.That(history.Target, Is.EqualTo(string.Empty));
    }

    [Test, AutoData]
    public void ClearDomainEvents_ShouldClearEvents(
        [WithValidDistributionHistorySuccess] DistributionHistory history)
    {
        Assert.That(history.DomainEvents, Is.Not.Empty);

        history.ClearDomainEvents();

        Assert.That(history.DomainEvents, Is.Empty);
    }
}