using AutoFixture.NUnit3;
using LeadService.Domain.Entities;
using LeadService.Domain.Enums;
using LeadService.Domain.Events;
using LeadService.Tests.Common.Attributes;
using NUnit.Framework;

namespace LeadService.Tests.Domain.Entities;

/// <summary>
/// Тесты для Lead
/// </summary>
[Category("Domain")]
public class LeadTests
{
    private const string Email = "email@test.com";
    private const string Phone = "79999999999";

    #region Create

    [Test, AutoData]
    public void Create_WithValidData_ShouldCreateLeadInInitialStatus(
        Guid leadId, string source, string companyName)
    {
        var lead = Lead.Create(leadId, source, companyName, Email);

        Assert.That(lead.Id, Is.EqualTo(leadId));
        Assert.That(lead.Source, Is.EqualTo(source));
        Assert.That(lead.CompanyName.Value, Is.EqualTo(companyName));
        Assert.That(lead.Email.Value, Is.EqualTo(Email));
        Assert.That(lead.ContactPerson, Is.Null);
        Assert.That(lead.Phone, Is.Null);
        Assert.That(lead.ExternalLeadId, Is.Null);
        Assert.That(lead.Status, Is.EqualTo(LeadStatus.Initial));
        Assert.That(lead.Score, Is.Null);
        Assert.That(lead.CreatedAt, Is.EqualTo(lead.UpdatedAt).Within(TimeSpan.FromMilliseconds(1)));
        Assert.That(lead.CustomFields, Is.Empty);
        Assert.That(lead.DomainEvents, Has.Exactly(1).InstanceOf<LeadCreatedDomainEvent>());

        var createdEvent = lead.DomainEvents.First() as LeadCreatedDomainEvent;
        Assert.That(createdEvent!.LeadId, Is.EqualTo(lead.Id));
        Assert.That(createdEvent.Source, Is.EqualTo(source));
        Assert.That(createdEvent.CompanyName, Is.EqualTo(companyName));
        Assert.That(createdEvent.Email, Is.EqualTo(Email));
    }

    [Test, AutoData]
    public void Create_WithAllOptionalFields_ShouldSetThemCorrectly(
        Guid leadId, string source, string companyName, string externalLeadId,
        string contactPerson, Dictionary<string, string> customFields)
    {
        var lead = Lead.Create(
            leadId, source, companyName, Email, 
            externalLeadId, contactPerson, Phone, customFields);

        Assert.That(lead.ExternalLeadId, Is.EqualTo(externalLeadId));
        Assert.That(lead.ContactPerson, Is.EqualTo(contactPerson));
        Assert.That(lead.Phone!.Value, Is.EqualTo(Phone));
        Assert.That(lead.CustomFields.Count, Is.EqualTo(customFields.Count));
    }

    [Test, AutoData]
    public void Create_WithEmptyId_ShouldThrow(
        string source, string companyName)
    {
        var ex = Assert.Throws<ArgumentException>(() => 
            Lead.Create(Guid.Empty, source, companyName, Email));

        Assert.That(ex.Message, Does.Contain("Lead ID cannot be empty"));
    }

    [Test]
    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void Create_WithEmptySource_ShouldThrow(string? source)
    {
        var ex = Assert.Throws<ArgumentException>(() => 
            Lead.Create(Guid.NewGuid(), source!, string.Empty, Email));

        Assert.That(ex.Message, Does.Contain("Source cannot be empty"));
    }

    #endregion

    #region MarkEnrichmentReceived

    [Test, AutoData]
    public void MarkEnrichmentReceived_WhenInInitialStatus_ShouldMarkAsReceived(
        [WithValidLead] Lead lead,
        string enrichedData)
    {
        lead.MarkEnrichmentReceived(enrichedData);

        Assert.That(lead.IsEnrichmentReceived, Is.True);
        Assert.That(lead.EnrichedData, Is.EqualTo(enrichedData));
        Assert.That(lead.UpdatedAt, Is.GreaterThan(lead.CreatedAt));
        Assert.That(lead.DomainEvents, Has.Exactly(2).Items);
        Assert.That(lead.DomainEvents, Has.Exactly(1).InstanceOf<EnrichmentReceivedDomainEvent>());
    }

    [Test, AutoData]
    public void MarkEnrichmentReceived_WhenAlreadyReceived_ShouldDoNothing(
        [WithValidLead] Lead lead,
        string fistData,
        string secondData)
    {
        lead.MarkEnrichmentReceived(fistData);
        var initialEventCount = lead.DomainEvents.Count;

        lead.MarkEnrichmentReceived(secondData);

        Assert.That(lead.IsEnrichmentReceived, Is.True);
        Assert.That(lead.EnrichedData, Is.EqualTo(fistData));
        Assert.That(lead.DomainEvents.Count, Is.EqualTo(initialEventCount));
    }

    [Test, AutoData]
    public void MarkEnrichmentReceived_WhenNotInInitialStatus_ShouldThrow(
        [WithValidLead(LeadStatus.Qualified)] Lead lead,
        string enrichedData)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => 
            lead.MarkEnrichmentReceived(enrichedData));

        Assert.That(ex.Message, Does.Contain("Cannot receive enrichment in status Qualified"));
    }
    #endregion

    #region MarkScoringReceived

    [Test, AutoData]
    public void MarkScoringReceived_WhenInInitialStatus_ShouldMarkAsReceived(
        [WithValidLead] Lead lead,
        int score)
    {
        lead.MarkScoringReceived(score);

        Assert.That(lead.IsScoringReceived, Is.True);
        Assert.That(lead.Score, Is.EqualTo(score));
        Assert.That(lead.UpdatedAt, Is.GreaterThan(lead.CreatedAt));
        Assert.That(lead.DomainEvents, Has.Exactly(2).Items);
        Assert.That(lead.DomainEvents, Has.Exactly(1).InstanceOf<ScoringReceivedDomainEvent>());
    }

    [Test, AutoData]
    public void MarkScoringReceived_WhenAlreadyReceived_ShouldDoNothing(
        [WithValidLead] Lead lead,
        int score)
    {
        lead.MarkScoringReceived(score);
        var initialEventCount = lead.DomainEvents.Count;

        lead.MarkScoringReceived(score + 1);

        Assert.That(lead.IsScoringReceived, Is.True);
        Assert.That(lead.Score, Is.EqualTo(score));
        Assert.That(lead.DomainEvents.Count, Is.EqualTo(initialEventCount));
    }

    [Test, AutoData]
    public void MarkScoringReceived_WhenNotInInitialStatus_ShouldThrow(
        [WithValidLead(LeadStatus.Qualified)] Lead lead,
        int score)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => 
            lead.MarkScoringReceived(score));

        Assert.That(ex.Message, Does.Contain("Cannot receive scoring in status Qualified"));
    }
    #endregion

    #region TryQualify

    [Test, AutoData]
    public void TryQualify_WhenBothReceived_ShouldTransitionToQualified(
        [WithValidLead] Lead lead,
        int score)
    {
        var enrichedData = "{\"industry\":\"Tech\",\"companySize\":\"50-100\"}";

        lead.MarkEnrichmentReceived(enrichedData);
        lead.MarkScoringReceived(score);

        Assert.That(lead.Status, Is.EqualTo(LeadStatus.Qualified));
        Assert.That(lead.DomainEvents, Has.Exactly(1).InstanceOf<LeadQualifiedDomainEvent>());

        var qualifiedEvent = lead.DomainEvents.OfType<LeadQualifiedDomainEvent>().First();
        Assert.That(qualifiedEvent.LeadId, Is.EqualTo(lead.Id));
        Assert.That(qualifiedEvent.Score, Is.EqualTo(score));
    }

    [Test, AutoData]
    public void TryQualify_WhenOnlyEnrichmentReceived_ShouldStayInInitial(
        [WithValidLead] Lead lead,
        string enrichedData)
    {
        lead.MarkEnrichmentReceived(enrichedData);

        Assert.That(lead.Status, Is.EqualTo(LeadStatus.Initial));
        Assert.That(lead.DomainEvents, Has.No.InstanceOf<LeadQualifiedDomainEvent>());
    }

    [Test, AutoData]
    public void TryQualify_WhenOnlyScoringReceived_ShouldStayInInitial(
        [WithValidLead] Lead lead,
        int score)
    {
        lead.MarkScoringReceived(score);

        Assert.That(lead.Status, Is.EqualTo(LeadStatus.Initial));
        Assert.That(lead.DomainEvents, Has.No.InstanceOf<LeadQualifiedDomainEvent>());
    }

    [Test, AutoData]
    public void TryQualify_WithInvalidEnrichedData_ShouldThrow(
        [WithValidLead] Lead lead,
        int score)
    {
        var invalidJson = "{invalidData}";
        lead.MarkEnrichmentReceived(invalidJson);

        var ex = Assert.Throws<InvalidOperationException>(() => 
            lead.MarkScoringReceived(score));

        Assert.That(ex.Message, Does.Contain("Failed to deserialize EnrichedData"));
    }

    #endregion

    #region Reject

    [Test, AutoData]
    public void Reject_WhenInInitial_ShouldTransitionToRejected(
        [WithValidLead] Lead lead,
        string reason, string failureType)
    {
        lead.Reject(reason, failureType);

        Assert.That(lead.Status, Is.EqualTo(LeadStatus.Rejected));
        Assert.That(lead.UpdatedAt, Is.GreaterThan(lead.CreatedAt));
        Assert.That(lead.DomainEvents, Has.Exactly(1).InstanceOf<LeadRejectedDomainEvent>());

        var rejectedEvent = lead.DomainEvents.OfType<LeadRejectedDomainEvent>().First();
        Assert.That(rejectedEvent.Reason, Is.EqualTo(reason));
        Assert.That(rejectedEvent.FailureType, Is.EqualTo(failureType));
    }

    [Test, AutoData]
    public void Reject_WhenAlreadyRejected_ShouldDoNothing(
        [WithValidLead(LeadStatus.Rejected)] Lead lead,
        string reason, string failureType)
    {
        lead.ClearDomainEvents();
        var initialEventCount = lead.DomainEvents.Count;

        lead.Reject(reason, failureType);

        Assert.That(lead.Status, Is.EqualTo(LeadStatus.Rejected));
        Assert.That(lead.DomainEvents.Count, Is.EqualTo(initialEventCount));
    }

    [Test, AutoData]
    public void Reject_WhenNotInInitialStatus_ShouldThrow(
        [WithValidLead(LeadStatus.Qualified)] Lead lead,
        string reason, string failureType)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => 
            lead.Reject(reason, failureType));

        Assert.That(ex.Message, Does.Contain("Cannot reject lead from status Qualified"));
    }
    #endregion

    #region MarkAsDistributed

    [Test, AutoData]
    public void MarkAsDistributed_WhenNotQualified_ShouldThrow(
        [WithValidLead] Lead lead,
        string target)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => 
            lead.MarkAsDistributed(target));

        Assert.That(ex.Message, Does.Contain("Cannot distribute lead from status Initial"));
    }

    [Test, AutoData]
    public void MarkAsDistributed_WhenAlreadyDistributed_ShouldDoNothing(
        [WithValidLead(LeadStatus.Distributed)] Lead lead,
        string target)
    {
        lead.ClearDomainEvents();
        var initialEventCount = lead.DomainEvents.Count;

        lead.MarkAsDistributed(target);

        Assert.That(lead.Status, Is.EqualTo(LeadStatus.Distributed));
        Assert.That(lead.DomainEvents.Count, Is.EqualTo(initialEventCount));
    }
    #endregion

    #region MarkDistributionFailed

    [Test, AutoData]
    public void MarkDistributionFailed_WhenNotQualified_ShouldThrow(
        [WithValidLead] Lead lead,
        string reason)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => 
            lead.MarkDistributionFailed(reason));

        Assert.That(ex.Message, Does.Contain("Cannot fail distribution from status Initial"));
    }

    [Test, AutoData]
    public void MarkDistributionFailed_WhenAlreadyFailed_ShouldDoNothing(
        [WithValidLead(LeadStatus.FailedDistribution)] Lead lead,
        string reason)
    {
        lead.ClearDomainEvents();
        var initialEventCount = lead.DomainEvents.Count;

        lead.MarkDistributionFailed(reason);

        Assert.That(lead.Status, Is.EqualTo(LeadStatus.FailedDistribution));
        Assert.That(lead.DomainEvents.Count, Is.EqualTo(initialEventCount));
    }
    #endregion

    #region Compensation

    [Test, AutoData]
    public void MarkEnrichmentCompensated_WhenInitial_ShouldThrow(
        [WithValidLead] Lead lead)
    {
        var ex = Assert.Throws<InvalidOperationException>(lead.MarkEnrichmentCompensated);

        Assert.That(ex.Message, Does.Contain("Cannot receive enrichment compensation in status Initial"));
    }

    [Test, AutoData]
    public void TryCloseAfterCompensation_WhenBothCompensated_ShouldClose(
        [WithValidLead(LeadStatus.Rejected)] Lead lead)
    {
        lead.MarkEnrichmentCompensated();
        lead.MarkScoringCompensated();

        Assert.That(lead.Status, Is.EqualTo(LeadStatus.Closed));
        Assert.That(lead.DomainEvents, Has.Exactly(1).InstanceOf<LeadClosedDomainEvent>());

        var closedEvent = lead.DomainEvents.OfType<LeadClosedDomainEvent>().First();
        Assert.That(closedEvent.PreviousStatus, Is.EqualTo(LeadStatus.Rejected));
    }

    [Test, AutoData]
    public void TryCloseAfterCompensation_WhenOnlyOneCompensated_ShouldNotClose(
        [WithValidLead(LeadStatus.Rejected)] Lead lead)
    {
        lead.MarkEnrichmentCompensated();

        Assert.That(lead.Status, Is.EqualTo(LeadStatus.Rejected));
        Assert.That(lead.DomainEvents, Has.No.InstanceOf<LeadClosedDomainEvent>());
    }

    [Test, AutoData]
    public void MarkEnrichmentCompensated_WhenAlreadyCompensated_ShouldDoNothing(
        [WithValidLead(LeadStatus.Rejected)] Lead lead)
    {
        lead.MarkEnrichmentCompensated();
        lead.ClearDomainEvents();
        var initialEventCount = lead.DomainEvents.Count;

        lead.MarkEnrichmentCompensated();

        Assert.That(lead.IsEnrichmentCompensated, Is.True);
        Assert.That(lead.DomainEvents.Count, Is.EqualTo(initialEventCount));
    }

    [Test, AutoData]
    public void MarkScoringCompensated_WhenAlreadyCompensated_ShouldDoNothing(
        [WithValidLead(LeadStatus.Rejected)] Lead lead)
    {
        lead.MarkScoringCompensated();
        lead.ClearDomainEvents();
        var initialEventCount = lead.DomainEvents.Count;

        lead.MarkScoringCompensated();

        Assert.That(lead.IsScoringCompensated, Is.True);
        Assert.That(lead.DomainEvents.Count, Is.EqualTo(initialEventCount));
    }

    [Test, AutoData]
    public void MarkScoringCompensated_WhenNotInRejectedOrFailed_ShouldThrow(
        [WithValidLead(LeadStatus.Qualified)] Lead lead)
    {
        var ex = Assert.Throws<InvalidOperationException>(lead.MarkScoringCompensated);

        Assert.That(ex.Message, Does.Contain("Cannot receive scoring compensation in status Qualified"));
    }

    [Test, AutoData]
    public void TryQualify_WithNullEnrichedData_ShouldLogDebugAndContinue(
        [WithValidLead] Lead lead,
        int score)
    {
        var enrichedData = "null";

        lead.MarkEnrichmentReceived(enrichedData);
        lead.MarkScoringReceived(score);

        Assert.That(lead.Status, Is.EqualTo(LeadStatus.Qualified));
        var qualifiedEvent = lead.DomainEvents.OfType<LeadQualifiedDomainEvent>().First();
        Assert.That(qualifiedEvent.EnrichedData, Is.Null);
    }
    #endregion

    #region CloseAfterDistribution

    [Test, AutoData]
    public void CloseAfterDistribution_WhenDistributed_ShouldClose(
        [WithValidLead(LeadStatus.Distributed)] Lead lead)
    {
        lead.ClearDomainEvents();

        lead.CloseAfterDistribution();

        Assert.That(lead.Status, Is.EqualTo(LeadStatus.Closed));
        Assert.That(lead.DomainEvents, Has.Exactly(1).InstanceOf<LeadClosedDomainEvent>());

        var closedEvent = lead.DomainEvents.OfType<LeadClosedDomainEvent>().First();
        Assert.That(closedEvent.PreviousStatus, Is.EqualTo(LeadStatus.Distributed));
    }

    [Test, AutoData]
    public void CloseAfterDistribution_WhenNotDistributed_ShouldThrow(
        [WithValidLead(LeadStatus.Qualified)] Lead lead)
    {
        var ex = Assert.Throws<InvalidOperationException>(lead.CloseAfterDistribution);

        Assert.That(ex.Message, Does.Contain("Cannot close lead from status Qualified"));
    }
    #endregion

    #region UpdateTimestamp

    [Test, AutoData]
    public void UpdateTimestamp_ShouldUpdateUpdatedAt(
        [WithValidLead] Lead lead)
    {
        var originalUpdatedAt = lead.UpdatedAt;

        Thread.Sleep(10);
        lead.UpdateTimestamp();

        Assert.That(lead.UpdatedAt, Is.GreaterThan(originalUpdatedAt));
    }

    #endregion
}