using AutoFixture.NUnit3;
using EnrichmentService.Domain.Entities;
using EnrichmentService.Domain.Enums;
using EnrichmentService.Domain.Events;
using EnrichmentService.Tests.Common.Attributes;
using NUnit.Framework;

namespace EnrichmentService.Tests.Domain.Entities;

/// <summary>
/// Тесты для EnrichmentRequest
/// </summary>
[Category("Domain")]
public class EnrichmentRequestTests
{
    #region Create

    [Test, AutoData]
    public void Create_WithValidData_ShouldCreateRequest(
        Guid leadId,
        string companyName,
        string email,
        string contactPerson,
        Dictionary<string, string> customFields)
    {
        var request = EnrichmentRequest.Create(
            leadId, companyName, email, contactPerson, customFields);

        Assert.That(request.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(request.LeadId, Is.EqualTo(leadId));
        Assert.That(request.CompanyName, Is.EqualTo(companyName));
        Assert.That(request.Email, Is.EqualTo(email));
        Assert.That(request.ContactPerson, Is.EqualTo(contactPerson));
        Assert.That(request.CustomFields, Is.EqualTo(customFields));
        Assert.That(request.Status, Is.EqualTo(EnrichmentRequestStatus.Pending));
        Assert.That(request.RetryCount, Is.EqualTo(0));
        Assert.That(request.LastAttemptAt, Is.Null);
        Assert.That(request.ErrorMessage, Is.Null);
        Assert.That(request.DomainEvents, Is.Empty);
    }

    [Test, AutoData]
    public void Create_WithNullCustomFields_ShouldSetToNull(
        Guid leadId,
        string companyName,
        string email,
        string contactPerson)
    {
        var request = EnrichmentRequest.Create(
            leadId, companyName, email, contactPerson, null);

        Assert.That(request.CustomFields, Is.Null);
    }

    #endregion

    #region StartProcessing

    [Test, AutoData]
    public void StartProcessing_ShouldChangeStatusToProcessing(
        [WithValidEnrichmentRequest] EnrichmentRequest request)
    {
        request.StartProcessing();

        Assert.That(request.Status, Is.EqualTo(EnrichmentRequestStatus.Processing));
        Assert.That(request.LastAttemptAt,
            Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
    }

    [Test, AutoData]
    public void StartProcessing_WhenAlreadyProcessing_ShouldThrow(
        [WithValidEnrichmentRequest] EnrichmentRequest request)
    {
        request.StartProcessing();

        var ex = Assert.Throws<InvalidOperationException>(request.StartProcessing);

        Assert.That(ex.Message,
            Does.Contain("Cannot start processing an enrichment request"));
    }

    #endregion

    #region MarkFailed

    [Test, AutoData]
    public void MarkFailed_ShouldChangeStatusAndAddEvent(
        [WithValidEnrichmentRequest] EnrichmentRequest request,
        string errorMessage)
    {
        request.StartProcessing();
        request.ClearDomainEvents();

        request.MarkFailed(errorMessage);

        Assert.That(request.Status, Is.EqualTo(EnrichmentRequestStatus.Failed));
        Assert.That(request.LastAttemptAt, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
        Assert.That(request.ErrorMessage, Is.EqualTo(errorMessage));
        Assert.That(request.RetryCount, Is.EqualTo(1));
        Assert.That(request.DomainEvents, Has.Exactly(1).InstanceOf<LeadEnrichmentFailedDomainEvent>());

        var domainEvent = request.DomainEvents.First() as LeadEnrichmentFailedDomainEvent;
        Assert.That(domainEvent!.LeadId, Is.EqualTo(request.LeadId));
        Assert.That(domainEvent.Reason, Is.EqualTo(errorMessage));
        Assert.That(domainEvent.RetryCount, Is.EqualTo(1));
    }

    [Test, AutoData]
    public void MarkFailed_MultipleTimes_ShouldIncrementRetryCount(
        [WithValidEnrichmentRequest] EnrichmentRequest request,
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

    #endregion

    #region CanRetry

    [Test, AutoData]
    public void CanRetry_WhenRetryCountLessThanMax_ShouldReturnTrue(
        [WithValidEnrichmentRequest] EnrichmentRequest request,
        string errorMessage)
    {
        var maxRetries = 3;

        for (int i = 0; i < maxRetries - 1; i++)
        {
            request.StartProcessing();
            request.MarkFailed(errorMessage);
        }

        Assert.That(request.CanRetry(maxRetries), Is.True);
    }

    [Test, AutoData]
    public void CanRetry_WhenRetryCountEqualToMax_ShouldReturnFalse(
        [WithValidEnrichmentRequest] EnrichmentRequest request,
        string errorMessage)
    {
        var maxRetries = 3;

        for (int i = 0; i < maxRetries; i++)
        {
            request.StartProcessing();
            request.MarkFailed(errorMessage);
        }

        Assert.That(request.CanRetry(maxRetries), Is.False);
    }

    [Test, AutoData]
    public void CanRetry_WhenRetryCountGreaterThanMax_ShouldReturnFalse(
        [WithValidEnrichmentRequest] EnrichmentRequest request,
        string errorMessage)
    {
        var maxRetries = 2;

        for (int i = 0; i < maxRetries + 1; i++)
        {
            request.StartProcessing();
            request.MarkFailed(errorMessage);
        }

        Assert.That(request.CanRetry(maxRetries), Is.False);
    }

    #endregion

    #region IsReadyForProcessing

    [Test, AutoData]
    public void IsReadyForProcessing_WhenPending_ShouldReturnTrue(
        [WithValidEnrichmentRequest] EnrichmentRequest request)
    {
        Assert.That(request.IsReadyForProcessing(3), Is.True);
    }

    [Test, AutoData]
    public void IsReadyForProcessing_WhenProcessing_ShouldReturnFalse(
        [WithValidEnrichmentRequest] EnrichmentRequest request)
    {
        request.StartProcessing();

        Assert.That(request.IsReadyForProcessing(3), Is.False);
    }

    [Test, AutoData]
    public void IsReadyForProcessing_WhenCompleted_ShouldReturnFalse(
        [WithValidEnrichmentRequest] EnrichmentRequest request)
    {
        request.StartProcessing();
        request.MarkCompleted();

        Assert.That(request.IsReadyForProcessing(3), Is.False);
    }

    [Test, AutoData]
    public void IsReadyForProcessing_WhenFailedAndCanRetry_ShouldReturnTrue(
        [WithValidEnrichmentRequest] EnrichmentRequest request,
        string errorMessage)
    {
        request.StartProcessing();
        request.MarkFailed(errorMessage);

        Assert.That(request.IsReadyForProcessing(3), Is.True);
    }

    [Test, AutoData]
    public void IsReadyForProcessing_WhenFailedAndCannotRetry_ShouldReturnFalse(
        [WithValidEnrichmentRequest] EnrichmentRequest request,
        string errorMessage)
    {
        var maxRetries = 2;

        for (int i = 0; i < maxRetries; i++)
        {
            request.StartProcessing();
            request.MarkFailed(errorMessage);
        }

        Assert.That(request.IsReadyForProcessing(maxRetries), Is.False);
    }

    #endregion
}