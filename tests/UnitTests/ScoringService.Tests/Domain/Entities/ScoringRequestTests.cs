using AutoFixture.NUnit4;
using NUnit.Framework;
using ScoringService.Domain.Entities;
using ScoringService.Domain.Enums;
using ScoringService.Domain.Events;
using ScoringService.Tests.Common.Attributes;

namespace ScoringService.Tests.Domain.Entities;

/// <summary>
/// Тесты для ScoringRequest
/// </summary>
[Category("Domain")]
public class ScoringRequestTests
{
    [Test, AutoData]
    public void Create_WithValidData_ShouldCreateRequest(
        Guid leadId,
        string companyName,
        string email,
        string contactPerson,
        Dictionary<string, string> customFields,
        string enrichedData)
    {
        var request = ScoringRequest.Create(leadId, companyName, email, contactPerson, customFields, enrichedData);

        Assert.That(request.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(request.LeadId, Is.EqualTo(leadId));
        Assert.That(request.CompanyName, Is.EqualTo(companyName));
        Assert.That(request.Email, Is.EqualTo(email));
        Assert.That(request.ContactPerson, Is.EqualTo(contactPerson));
        Assert.That(request.CustomFields, Is.EqualTo(customFields));
        Assert.That(request.EnrichedData, Is.EqualTo(enrichedData));
        Assert.That(request.Status, Is.EqualTo(ScoringRequestStatus.Pending));
        Assert.That(request.RetryCount, Is.EqualTo(0));
        Assert.That(request.LastAttemptAt, Is.Null);
        Assert.That(request.ErrorMessage, Is.Null);
        Assert.That(request.DomainEvents, Is.Empty);
    }

    [Test, AutoData]
    public void UpdateEnrichedData_WhenPending_ShouldUpdate(
        [WithValidScoringRequest] ScoringRequest request,
        string newEnrichedData)
    {
        request.UpdateEnrichedData(newEnrichedData);

        Assert.That(request.EnrichedData, Is.EqualTo(newEnrichedData));
    }

    [Test, AutoData]
    public void UpdateEnrichedData_WhenCompleted_ShouldThrow(
        [WithValidScoringRequest] ScoringRequest request,
        string newEnrichedData,
        int score,
        int threshold,
        List<string> appliedRules)
    {
        request.StartProcessing();
        request.MarkCompleted(score, threshold, appliedRules);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            request.UpdateEnrichedData(newEnrichedData));

        Assert.That(ex.Message, Does.Contain("Cannot update enriched data"));
    }

    [Test, AutoData]
    public void StartProcessing_ShouldChangeStatusToProcessing(
        [WithValidScoringRequest] ScoringRequest request)
    {
        request.StartProcessing();

        Assert.That(request.Status, Is.EqualTo(ScoringRequestStatus.Processing));
        Assert.That(request.LastAttemptAt, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
    }

    [Test, AutoData]
    public void StartProcessing_WhenAlreadyProcessing_ShouldThrow(
        [WithValidScoringRequest] ScoringRequest request)
    {
        request.StartProcessing();

        var ex = Assert.Throws<InvalidOperationException>(request.StartProcessing);

        Assert.That(ex.Message, Does.Contain("Cannot start processing"));
    }

    [Test, AutoData]
    public void MarkCompleted_ShouldChangeStatusAndAddEvent(
        [WithValidScoringRequest] ScoringRequest request,
        int score,
        int threshold,
        List<string> appliedRules)
    {
        request.StartProcessing();
        request.ClearDomainEvents();

        request.MarkCompleted(score, threshold, appliedRules);

        Assert.That(request.Status, Is.EqualTo(ScoringRequestStatus.Completed));
        Assert.That(request.LastAttemptAt, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
        Assert.That(request.DomainEvents, Has.Exactly(1).InstanceOf<LeadScoredDomainEvent>());

        var domainEvent = request.DomainEvents.First() as LeadScoredDomainEvent;
        Assert.That(domainEvent!.LeadId, Is.EqualTo(request.LeadId));
        Assert.That(domainEvent.TotalScore, Is.EqualTo(score));
        Assert.That(domainEvent.QualifiedThreshold, Is.EqualTo(threshold));
        Assert.That(domainEvent.AppliedRules, Is.EquivalentTo(appliedRules));
    }

    [Test, AutoData]
    public void MarkFailed_ShouldChangeStatusAndAddEvent(
        [WithValidScoringRequest] ScoringRequest request,
        string errorMessage)
    {
        request.StartProcessing();
        request.ClearDomainEvents();

        request.MarkFailed(errorMessage);

        Assert.That(request.Status, Is.EqualTo(ScoringRequestStatus.Failed));
        Assert.That(request.LastAttemptAt, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
        Assert.That(request.ErrorMessage, Is.EqualTo(errorMessage));
        Assert.That(request.RetryCount, Is.EqualTo(1));
        Assert.That(request.DomainEvents, Has.Exactly(1).InstanceOf<LeadScoringFailedDomainEvent>());

        var domainEvent = request.DomainEvents.First() as LeadScoringFailedDomainEvent;
        Assert.That(domainEvent!.LeadId, Is.EqualTo(request.LeadId));
        Assert.That(domainEvent.Reason, Is.EqualTo(errorMessage));
        Assert.That(domainEvent.RetryCount, Is.EqualTo(1));
    }

    [Test, AutoData]
    public void IsReadyForProcessing_WhenPending_ShouldReturnTrue(
        [WithValidScoringRequest] ScoringRequest request)
    {
        Assert.That(request.IsReadyForProcessing(3), Is.True);
    }

    [Test, AutoData]
    public void IsReadyForProcessing_WhenProcessing_ShouldReturnFalse(
        [WithValidScoringRequest] ScoringRequest request)
    {
        request.StartProcessing();

        Assert.That(request.IsReadyForProcessing(3), Is.False);
    }

    [Test, AutoData]
    public void IsReadyForProcessing_WhenFailedAndCanRetry_ShouldReturnTrue(
        [WithValidScoringRequest] ScoringRequest request,
        string errorMessage)
    {
        request.StartProcessing();
        request.MarkFailed(errorMessage);

        Assert.That(request.IsReadyForProcessing(3), Is.True);
    }

    [Test, AutoData]
    public void ClearEnrichedData_ShouldSetToNull(
        [WithValidScoringRequest] ScoringRequest request)
    {
        request.ClearEnrichedData();

        Assert.That(request.EnrichedData, Is.Null);
    }

    [Test, AutoData]
    public void MarkCompleted_WhenInPendingState_ShouldThrow(
        [WithValidScoringRequest] ScoringRequest request,
        int score,
        int threshold,
        List<string> appliedRules)
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            request.MarkCompleted(score, threshold, appliedRules));

        Assert.That(ex.Message, Does.Contain("Cannot complete a scoring request"));
        Assert.That(ex.Message, Does.Contain($"Current state: {ScoringRequestStatus.Pending}"));
        Assert.That(request.Status, Is.EqualTo(ScoringRequestStatus.Pending));
        Assert.That(request.DomainEvents, Is.Empty);
    }

    [Test, AutoData]
    public void MarkCompleted_WhenInFailedState_ShouldThrow(
        [WithValidScoringRequest] ScoringRequest request,
        string errorMessage,
        int score,
        int threshold,
        List<string> appliedRules)
    {
        request.StartProcessing();
        request.MarkFailed(errorMessage);
        request.ClearDomainEvents();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            request.MarkCompleted(score, threshold, appliedRules));

        Assert.That(ex.Message, Does.Contain("Cannot complete a scoring request"));
        Assert.That(ex.Message, Does.Contain($"Current state: {ScoringRequestStatus.Failed}"));
        Assert.That(request.Status, Is.EqualTo(ScoringRequestStatus.Failed));
        Assert.That(request.DomainEvents, Is.Empty);
    }

    [Test, AutoData]
    public void MarkCompleted_WhenInCompletedState_ShouldThrow(
        [WithValidScoringRequest] ScoringRequest request,
        int score,
        int threshold,
        List<string> appliedRules)
    {
        request.StartProcessing();
        request.MarkCompleted(score, threshold, appliedRules);
        request.ClearDomainEvents();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            request.MarkCompleted(score, threshold, appliedRules));

        Assert.That(ex.Message, Does.Contain("Cannot complete a scoring request"));
        Assert.That(ex.Message, Does.Contain($"Current state: {ScoringRequestStatus.Completed}"));
        Assert.That(request.Status, Is.EqualTo(ScoringRequestStatus.Completed));
        Assert.That(request.DomainEvents, Is.Empty);
    }

    [Test, AutoData]
    public void MarkFailed_WhenInPendingState_ShouldThrow(
        [WithValidScoringRequest] ScoringRequest request,
        string errorMessage)
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            request.MarkFailed(errorMessage));

        Assert.That(ex.Message, Does.Contain("Cannot mark as failed a scoring request"));
        Assert.That(ex.Message, Does.Contain($"Current state: {ScoringRequestStatus.Pending}"));
        Assert.That(request.Status, Is.EqualTo(ScoringRequestStatus.Pending));
        Assert.That(request.RetryCount, Is.EqualTo(0));
        Assert.That(request.ErrorMessage, Is.Null);
        Assert.That(request.DomainEvents, Is.Empty);
    }

    [Test, AutoData]
    public void MarkFailed_WhenInCompletedState_ShouldThrow(
        [WithValidScoringRequest] ScoringRequest request,
        int score,
        int threshold,
        List<string> appliedRules,
        string errorMessage)
    {
        request.StartProcessing();
        request.MarkCompleted(score, threshold, appliedRules);
        request.ClearDomainEvents();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            request.MarkFailed(errorMessage));

        Assert.That(ex.Message, Does.Contain("Cannot mark as failed a scoring request"));
        Assert.That(ex.Message, Does.Contain($"Current state: {ScoringRequestStatus.Completed}"));
        Assert.That(request.Status, Is.EqualTo(ScoringRequestStatus.Completed));
        Assert.That(request.RetryCount, Is.EqualTo(0));
        Assert.That(request.ErrorMessage, Is.Null);
        Assert.That(request.DomainEvents, Is.Empty);
    }

    [Test, AutoData]
    public void MarkFailed_WhenAlreadyFailed_ShouldThrow(
        [WithValidScoringRequest] ScoringRequest request,
        string errorMessage1,
        string errorMessage2)
    {
        request.StartProcessing();
        request.MarkFailed(errorMessage1);
        request.ClearDomainEvents();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            request.MarkFailed(errorMessage2));

        Assert.That(ex.Message, Does.Contain("Cannot mark as failed a scoring request"));
        Assert.That(ex.Message, Does.Contain($"Current state: {ScoringRequestStatus.Failed}"));
        Assert.That(request.Status, Is.EqualTo(ScoringRequestStatus.Failed));
        Assert.That(request.RetryCount, Is.EqualTo(1));
        Assert.That(request.ErrorMessage, Is.EqualTo(errorMessage1));
        Assert.That(request.DomainEvents, Is.Empty);
    }
}