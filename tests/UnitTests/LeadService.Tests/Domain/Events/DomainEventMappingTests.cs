using AutoFixture.NUnit4;
using AvroSchemas.Messages.LeadEvents;
using LeadService.Domain.Enums;
using LeadService.Domain.Events;
using NUnit.Framework;

namespace LeadService.Tests.Domain.Events;

/// <summary>
/// Тесты для доменных событий
/// </summary>
[Category("Domain")]
public class DomainEventMappingTests
{
    [Test, AutoData]
    public void LeadCreatedDomainEvent_ShouldSetProperties(
        Guid  leadId, string source, string companyName,
        string contactPerson, string email, string phone,
        string externalLeadId, Dictionary<string, string> customFields)
    {
        var @event = new LeadCreatedDomainEvent(
            leadId, source, companyName, contactPerson, email, phone, externalLeadId, customFields);

        Assert.That(@event.LeadId, Is.EqualTo(leadId));
        Assert.That(@event.Source, Is.EqualTo(source));
        Assert.That(@event.CompanyName, Is.EqualTo(companyName));
        Assert.That(@event.ContactPerson, Is.EqualTo(contactPerson));
        Assert.That(@event.Email, Is.EqualTo(email));
        Assert.That(@event.Phone, Is.EqualTo(phone));
        Assert.That(@event.ExternalLeadId, Is.EqualTo(externalLeadId));
        Assert.That(@event.CustomFields, Is.EqualTo(customFields));
        Assert.That(@event.EventId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(@event.OccurredOn, Is.LessThanOrEqualTo(DateTime.UtcNow));
    }

    [Test, AutoData]
    public void LeadQualifiedDomainEvent_ShouldSetProperties(
        Guid leadId, int score, string companyName, string contactPerson, string email,
        EnrichedDataDto enrichedData)
    {
        var @event = new LeadQualifiedDomainEvent(
            leadId, score, companyName, contactPerson, email, enrichedData);

        Assert.That(@event.LeadId, Is.EqualTo(leadId));
        Assert.That(@event.Score, Is.EqualTo(score));
        Assert.That(@event.CompanyName, Is.EqualTo(companyName));
        Assert.That(@event.ContactPerson, Is.EqualTo(contactPerson));
        Assert.That(@event.Email, Is.EqualTo(email));
        Assert.That(@event.EnrichedData, Is.EqualTo(enrichedData));
    }

    [Test, AutoData]
    public void LeadRejectedDomainEvent_ShouldSetProperties(
        Guid leadId, string reason, string failureType)
    {
        var @event = new LeadRejectedDomainEvent(leadId, reason, failureType);

        Assert.That(@event.LeadId, Is.EqualTo(leadId));
        Assert.That(@event.Reason, Is.EqualTo(reason));
        Assert.That(@event.FailureType, Is.EqualTo(failureType));
    }

    [Test, AutoData]
    public void LeadDistributedDomainEvent_ShouldSetProperties(
        Guid leadId, string target)
    {
        var @event = new LeadDistributedDomainEvent(leadId, target);

        Assert.That(@event.LeadId, Is.EqualTo(leadId));
        Assert.That(@event.Target, Is.EqualTo(target));
    }

    [Test, AutoData]
    public void LeadDistributionFailedDomainEvent_ShouldSetProperties(
        Guid leadId, string reason)
    {
        var @event = new LeadDistributionFailedDomainEvent(leadId, reason);

        Assert.That(@event.LeadId, Is.EqualTo(leadId));
        Assert.That(@event.Reason, Is.EqualTo(reason));
    }

    [Test, AutoData]
    public void EnrichmentReceivedDomainEvent_ShouldSetProperties(
        Guid leadId)
    {
        var @event = new EnrichmentReceivedDomainEvent(leadId);

        Assert.That(@event.LeadId, Is.EqualTo(leadId));
    }

    [Test, AutoData]
    public void ScoringReceivedDomainEvent_ShouldSetProperties(
        Guid leadId)
    {
        var @event = new ScoringReceivedDomainEvent(leadId);

        Assert.That(@event.LeadId, Is.EqualTo(leadId));
    }

    [Test, AutoData]
    public void EnrichmentCompensatedDomainEvent_ShouldSetProperties(
        Guid leadId)
    {
        var @event = new EnrichmentCompensatedDomainEvent(leadId);

        Assert.That(@event.LeadId, Is.EqualTo(leadId));
    }

    [Test, AutoData]
    public void ScoringCompensatedDomainEvent_ShouldSetProperties(
        Guid leadId)
    {
        var @event = new ScoringCompensatedDomainEvent(leadId);

        Assert.That(@event.LeadId, Is.EqualTo(leadId));
    }

    [Test, AutoData]
    public void LeadClosedDomainEvent_ShouldSetProperties(
        Guid leadId)
    {
        var previousStatus = LeadStatus.Distributed;

        var @event = new LeadClosedDomainEvent(leadId, previousStatus);

        Assert.That(@event.LeadId, Is.EqualTo(leadId));
        Assert.That(@event.PreviousStatus, Is.EqualTo(previousStatus));
    }
}