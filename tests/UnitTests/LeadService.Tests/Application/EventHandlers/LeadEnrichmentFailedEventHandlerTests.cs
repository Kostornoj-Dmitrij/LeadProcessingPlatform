using AutoFixture.NUnit3;
using IntegrationEvents.EnrichmentEvents;
using LeadService.Application.EventHandlers;
using LeadService.Domain.Entities;
using LeadService.Domain.Enums;
using LeadService.Tests.Common.Attributes;
using LeadService.Tests.Common.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SharedKernel.Events;

namespace LeadService.Tests.Application.EventHandlers;

/// <summary>
/// Тесты для LeadEnrichmentFailedEventHandler
/// </summary>
[Category("Application")]
public class LeadEnrichmentFailedEventHandlerTests : DatabaseTestBase
{
    private Mock<ILogger<LeadEnrichmentFailedEventHandler>> _loggerMock = null!;
    private LeadEnrichmentFailedEventHandler _sut = null!;

    [SetUp]
    public void Setup()
    {
        BaseSetup();
        _loggerMock = new Mock<ILogger<LeadEnrichmentFailedEventHandler>>();
        _sut = new LeadEnrichmentFailedEventHandler(UnitOfWorkMock.Object, _loggerMock.Object);
    }

    [TearDown]
    public void Cleanup()
    {
        BaseCleanup();
        _loggerMock.Reset();
    }

    [Test, AutoData]
    public async Task Handle_WhenLeadExists_ShouldRejectLead(
        [WithValidLead] Lead lead,
        [WithLeadEnrichmentFailedEvent] LeadEnrichmentFailedIntegrationEvent integrationEvent)
    {
        var leadType = typeof(Lead);
        leadType.GetProperty(nameof(Lead.Id))?.SetValue(lead, integrationEvent.LeadId);

        var leads = new List<Lead> { lead };
        var leadSetMock = CreateMockDbSet(leads);

        UnitOfWorkMock
            .Setup(x => x.Set<Lead>())
            .Returns(leadSetMock.Object);

        var wrapper = new IntegrationEventWrapper<LeadEnrichmentFailedIntegrationEvent>(integrationEvent);

        await _sut.Handle(wrapper, CancellationToken.None);

        Assert.That(lead.Status, Is.EqualTo(LeadStatus.Rejected));
        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_WhenLeadNotFound_ShouldLogWarning(
        [WithLeadEnrichmentFailedEvent] LeadEnrichmentFailedIntegrationEvent integrationEvent)
    {
        var leads = new List<Lead>();
        var leadSetMock = CreateMockDbSet(leads);

        UnitOfWorkMock
            .Setup(x => x.Set<Lead>())
            .Returns(leadSetMock.Object);

        var wrapper = new IntegrationEventWrapper<LeadEnrichmentFailedIntegrationEvent>(integrationEvent);

        await _sut.Handle(wrapper, CancellationToken.None);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains($"Lead not found: {integrationEvent.LeadId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test, AutoData]
    public void Handle_WhenConcurrencyException_ShouldThrow(
        [WithValidLead] Lead lead,
        [WithLeadEnrichmentFailedEvent] LeadEnrichmentFailedIntegrationEvent integrationEvent)
    {
        var leadType = typeof(Lead);
        leadType.GetProperty(nameof(Lead.Id))?.SetValue(lead, integrationEvent.LeadId);

        var leads = new List<Lead> { lead };
        var leadSetMock = CreateMockDbSet(leads);

        UnitOfWorkMock
            .Setup(x => x.Set<Lead>())
            .Returns(leadSetMock.Object);

        UnitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        var wrapper = new IntegrationEventWrapper<LeadEnrichmentFailedIntegrationEvent>(integrationEvent);

        Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => 
            _sut.Handle(wrapper, CancellationToken.None));
    }

    [Test, AutoData]
    public void Handle_WhenGenericException_ShouldThrow(
        [WithValidLead] Lead lead,
        [WithLeadEnrichmentFailedEvent] LeadEnrichmentFailedIntegrationEvent integrationEvent,
        InvalidOperationException exception)
    {
        var leadType = typeof(Lead);
        leadType.GetProperty(nameof(Lead.Id))?.SetValue(lead, integrationEvent.LeadId);

        var leads = new List<Lead> { lead };
        var leadSetMock = CreateMockDbSet(leads);

        UnitOfWorkMock
            .Setup(x => x.Set<Lead>())
            .Returns(leadSetMock.Object);

        UnitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        var wrapper = new IntegrationEventWrapper<LeadEnrichmentFailedIntegrationEvent>(integrationEvent);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => 
            _sut.Handle(wrapper, CancellationToken.None));

        Assert.That(ex.Message, Is.EqualTo(exception.Message));
    }
}