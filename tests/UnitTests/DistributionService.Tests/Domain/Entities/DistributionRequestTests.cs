using AutoFixture.NUnit4;
using DistributionService.Domain.Entities;
using DistributionService.Domain.Enums;
using DistributionService.Domain.Events;
using DistributionService.Tests.Common.Attributes;
using NUnit.Framework;

namespace DistributionService.Tests.Domain.Entities;

/// <summary>
/// Тесты для DistributionRequest
/// </summary>
[Category("Domain")]
public class DistributionRequestTests
{
    #region Create

    [Test, AutoData]
    public void Create_WithValidData_ShouldCreateRequest(
        Guid leadId,
        string companyName,
        string email,
        int score,
        string contactPerson,
        Dictionary<string, string> customFields,
        string enrichedData,
        string traceParent)
    {
        var request = DistributionRequest.Create(
            leadId, companyName, email, score, contactPerson, customFields, enrichedData, traceParent);

        Assert.That(request.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(request.LeadId, Is.EqualTo(leadId));
        Assert.That(request.CompanyName, Is.EqualTo(companyName));
        Assert.That(request.Email, Is.EqualTo(email));
        Assert.That(request.Score, Is.EqualTo(score));
        Assert.That(request.ContactPerson, Is.EqualTo(contactPerson));
        Assert.That(request.CustomFields, Is.EqualTo(customFields));
        Assert.That(request.EnrichedData, Is.EqualTo(enrichedData));
        Assert.That(request.TraceParent, Is.EqualTo(traceParent));
        Assert.That(request.Status, Is.EqualTo(DistributionRequestStatus.Pending));
        Assert.That(request.RetryCount, Is.EqualTo(0));
        Assert.That(request.LastAttemptAt, Is.Null);
        Assert.That(request.ErrorMessage, Is.Null);
        Assert.That(request.NextRetryAt, Is.Null);
        Assert.That(request.RuleId, Is.Null);
        Assert.That(request.Target, Is.Null);
        Assert.That(request.DomainEvents, Is.Empty);
    }

    [Test, AutoData]
    public void Create_WithNullOptionalFields_ShouldSetToNull(
        Guid leadId,
        string companyName,
        string email,
        int score)
    {
        var request = DistributionRequest.Create(
            leadId, companyName, email, score);

        Assert.That(request.ContactPerson, Is.Null);
        Assert.That(request.CustomFields, Is.Null);
        Assert.That(request.EnrichedData, Is.Null);
        Assert.That(request.TraceParent, Is.Null);
    }

    #endregion

    #region SetRuleAndTarget

    [Test, AutoData]
    public void SetRuleAndTarget_ShouldSetProperties(
        [WithValidDistributionRequest] DistributionRequest request,
        Guid ruleId,
        string target)
    {
        request.SetRuleAndTarget(ruleId, target);

        Assert.That(request.RuleId, Is.EqualTo(ruleId));
        Assert.That(request.Target, Is.EqualTo(target));
    }

    #endregion

    #region StartProcessing

    [Test, AutoData]
    public void StartProcessing_WhenPending_ShouldChangeStatusToProcessing(
        [WithValidDistributionRequest] DistributionRequest request)
    {
        request.StartProcessing();

        Assert.That(request.Status, Is.EqualTo(DistributionRequestStatus.Processing));
        Assert.That(request.LastAttemptAt,
            Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
    }

    [Test, AutoData]
    public void StartProcessing_WhenFailed_ShouldChangeStatusToProcessing(
        [WithValidDistributionRequest] DistributionRequest request,
        string errorMessage)
    {
        request.StartProcessing();
        request.MarkFailed(errorMessage);

        request.StartProcessing();

        Assert.That(request.Status, Is.EqualTo(DistributionRequestStatus.Processing));
        Assert.That(request.LastAttemptAt,
            Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
    }

    [Test, AutoData]
    public void StartProcessing_WhenAlreadyProcessing_ShouldThrow(
        [WithValidDistributionRequest] DistributionRequest request)
    {
        request.StartProcessing();

        var ex = Assert.Throws<InvalidOperationException>(request.StartProcessing);

        Assert.That(ex.Message,
            Does.Contain("Cannot start processing a distribution request"));
        Assert.That(ex.Message,
            Does.Contain($"Current state: {DistributionRequestStatus.Processing}"));
    }

    [Test, AutoData]
    public void StartProcessing_WhenCompleted_ShouldThrow(
        [WithValidDistributionRequest] DistributionRequest request)
    {
        request.StartProcessing();
        request.MarkCompleted();

        var ex = Assert.Throws<InvalidOperationException>(request.StartProcessing);

        Assert.That(ex.Message,
            Does.Contain("Cannot start processing a distribution request"));
        Assert.That(ex.Message,
            Does.Contain($"Current state: {DistributionRequestStatus.Completed}"));
    }

    #endregion

    #region MarkCompleted

    [Test, AutoData]
    public void MarkCompleted_WhenProcessing_ShouldChangeStatusToCompleted(
        [WithValidDistributionRequest] DistributionRequest request)
    {
        request.StartProcessing();
        request.ClearDomainEvents();

        request.MarkCompleted();

        Assert.That(request.Status, Is.EqualTo(DistributionRequestStatus.Completed));
        Assert.That(request.LastAttemptAt,
            Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
        Assert.That(request.NextRetryAt, Is.Null);
        Assert.That(request.DomainEvents, Is.Empty);
    }

    [Test, AutoData]
    public void MarkCompleted_WhenNotProcessing_ShouldThrow(
        [WithValidDistributionRequest] DistributionRequest request,
        string errorMessage,
        string companyName,
        string email,
        int score)
    {
        var ex1 = Assert.Throws<InvalidOperationException>(request.MarkCompleted);
        Assert.That(ex1.Message, Does.Contain("Cannot complete a distribution request"));
        Assert.That(ex1.Message, Does.Contain($"Current state: {DistributionRequestStatus.Pending}"));

        request.StartProcessing();
        request.MarkFailed(errorMessage);

        var ex2 = Assert.Throws<InvalidOperationException>(request.MarkCompleted);
        Assert.That(ex2.Message, Does.Contain("Cannot complete a distribution request"));
        Assert.That(ex2.Message, Does.Contain($"Current state: {DistributionRequestStatus.Failed}"));

        var completedRequest = DistributionRequest.Create(
            Guid.NewGuid(), companyName, email, score);
        completedRequest.StartProcessing();
        completedRequest.MarkCompleted();

        var ex3 = Assert.Throws<InvalidOperationException>(completedRequest.MarkCompleted);
        Assert.That(ex3.Message, Does.Contain("Cannot complete a distribution request"));
        Assert.That(ex3.Message, Does.Contain($"Current state: {DistributionRequestStatus.Completed}"));
    }

    #endregion

    #region MarkFailed

    [Test, AutoData]
    public void MarkFailed_WhenProcessing_ShouldChangeStatusAndAddEvent(
        [WithValidDistributionRequest] DistributionRequest request,
        string errorMessage,
        DateTime nextRetryAt)
    {
        request.StartProcessing();
        request.ClearDomainEvents();

        request.MarkFailed(errorMessage, nextRetryAt);

        Assert.That(request.Status, Is.EqualTo(DistributionRequestStatus.Failed));
        Assert.That(request.LastAttemptAt,
            Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
        Assert.That(request.ErrorMessage, Is.EqualTo(errorMessage));
        Assert.That(request.RetryCount, Is.EqualTo(1));
        Assert.That(request.NextRetryAt, Is.EqualTo(nextRetryAt));
        Assert.That(request.DomainEvents, Has.Exactly(1).InstanceOf<DistributionFailedDomainEvent>());

        var domainEvent = request.DomainEvents.First() as DistributionFailedDomainEvent;
        Assert.That(domainEvent!.LeadId, Is.EqualTo(request.LeadId));
        Assert.That(domainEvent.Reason, Is.EqualTo(errorMessage));
    }

    [Test, AutoData]
    public void MarkFailed_WithoutNextRetryAt_ShouldSetToNull(
        [WithValidDistributionRequest] DistributionRequest request,
        string errorMessage)
    {
        request.StartProcessing();
        request.ClearDomainEvents();

        request.MarkFailed(errorMessage);

        Assert.That(request.NextRetryAt, Is.Null);
    }

    [Test, AutoData]
    public void MarkFailed_MultipleTimes_ShouldIncrementRetryCount(
        [WithValidDistributionRequest] DistributionRequest request,
        string errorMessage1,
        string errorMessage2,
        string errorMessage3)
    {
        request.StartProcessing();
        request.MarkFailed(errorMessage1);
        Assert.That(request.RetryCount, Is.EqualTo(1));

        request.StartProcessing();
        request.MarkFailed(errorMessage2);
        Assert.That(request.RetryCount, Is.EqualTo(2));

        request.StartProcessing();
        request.MarkFailed(errorMessage3);
        Assert.That(request.RetryCount, Is.EqualTo(3));
    }

    [Test, AutoData]
    public void MarkFailed_WhenNotProcessing_ShouldThrow(
        [WithValidDistributionRequest] DistributionRequest request,
        string errorMessage,
        string companyName,
        string email,
        int score)
    {
        var ex1 = Assert.Throws<InvalidOperationException>(() =>
            request.MarkFailed(errorMessage));
        Assert.That(ex1.Message, Does.Contain("Cannot mark as failed"));
        Assert.That(ex1.Message, Does.Contain($"Current state: {DistributionRequestStatus.Pending}"));

        request.StartProcessing();
        request.MarkCompleted();

        var ex2 = Assert.Throws<InvalidOperationException>(() =>
            request.MarkFailed(errorMessage));
        Assert.That(ex2.Message, Does.Contain("Cannot mark as failed"));
        Assert.That(ex2.Message, Does.Contain($"Current state: {DistributionRequestStatus.Completed}"));

        var failedRequest = DistributionRequest.Create(
            Guid.NewGuid(), companyName, email, score);
        failedRequest.StartProcessing();
        failedRequest.MarkFailed(errorMessage);

        var ex3 = Assert.Throws<InvalidOperationException>(() =>
            failedRequest.MarkFailed(errorMessage));
        Assert.That(ex3.Message, Does.Contain("Cannot mark as failed"));
        Assert.That(ex3.Message, Does.Contain($"Current state: {DistributionRequestStatus.Failed}"));
    }

    #endregion

    #region ClearDomainEvents

    [Test, AutoData]
    public void ClearDomainEvents_ShouldClearEvents(
        [WithValidDistributionRequest] DistributionRequest request,
        string errorMessage)
    {
        request.StartProcessing();
        request.MarkFailed(errorMessage);

        Assert.That(request.DomainEvents, Is.Not.Empty);

        request.ClearDomainEvents();

        Assert.That(request.DomainEvents, Is.Empty);
    }

    #endregion
}