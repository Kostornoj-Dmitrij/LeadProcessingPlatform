using AutoFixture.NUnit3;
using IntegrationEvents.ScoringEvents;
using LeadService.Application.EventHandlers;
using LeadService.Domain.Entities;
using LeadService.Domain.Enums;
using LeadService.Tests.Common.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SharedKernel.Events;
using SharedTestInfrastructure.Database;

namespace LeadService.Tests.Application.EventHandlers;

/// <summary>
/// Тесты для LeadScoringFailedEventHandler
/// </summary>
[Category("Application")]
public class LeadScoringFailedEventHandlerTests : DatabaseTestBase
{
    private static readonly Type LeadType = typeof(Lead);

    private Mock<ILogger<LeadScoringFailedEventHandler>> _loggerMock = null!;
    private LeadScoringFailedEventHandler _sut = null!;

    [SetUp]
    public void Setup()
    {
        BaseSetup();
        _loggerMock = new Mock<ILogger<LeadScoringFailedEventHandler>>();
        _sut = new LeadScoringFailedEventHandler(UnitOfWorkMock.Object, _loggerMock.Object);
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
        [WithLeadScoringFailedEvent] LeadScoringFailedIntegrationEvent integrationEvent)
    {
        LeadType.GetProperty(nameof(Lead.Id))?.SetValue(lead, integrationEvent.LeadId);

        var leads = new List<Lead> { lead };
        var leadSetMock = CreateMockDbSet(leads);

        UnitOfWorkMock
            .Setup(x => x.Set<Lead>())
            .Returns(leadSetMock.Object);

        var wrapper = new IntegrationEventWrapper<LeadScoringFailedIntegrationEvent>(integrationEvent);

        await _sut.Handle(wrapper, CancellationToken.None);

        Assert.That(lead.Status, Is.EqualTo(LeadStatus.Rejected));
        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_WhenLeadNotFound_ShouldLogWarning(
        [WithLeadScoringFailedEvent] LeadScoringFailedIntegrationEvent integrationEvent)
    {
        var leads = new List<Lead>();
        var leadSetMock = CreateMockDbSet(leads);

        UnitOfWorkMock
            .Setup(x => x.Set<Lead>())
            .Returns(leadSetMock.Object);

        var wrapper = new IntegrationEventWrapper<LeadScoringFailedIntegrationEvent>(integrationEvent);

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
    }

    [Test, AutoData]
    public void Handle_WhenConcurrencyException_ShouldLogWarningAndThrow(
        [WithValidLead] Lead lead,
        [WithLeadScoringFailedEvent] LeadScoringFailedIntegrationEvent integrationEvent)
    {
        LeadType.GetProperty(nameof(Lead.Id))?.SetValue(lead, integrationEvent.LeadId);

        var leads = new List<Lead> { lead };
        var leadSetMock = CreateMockDbSet(leads);

        UnitOfWorkMock
            .Setup(x => x.Set<Lead>())
            .Returns(leadSetMock.Object);

        UnitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        var wrapper = new IntegrationEventWrapper<LeadScoringFailedIntegrationEvent>(integrationEvent);

        Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => 
            _sut.Handle(wrapper, CancellationToken.None));

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains($"Concurrency conflict for lead {integrationEvent.LeadId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test, AutoData]
    public void Handle_WhenGenericException_ShouldLogErrorAndThrow(
        [WithValidLead] Lead lead,
        [WithLeadScoringFailedEvent] LeadScoringFailedIntegrationEvent integrationEvent,
        InvalidOperationException exception)
    {
        LeadType.GetProperty(nameof(Lead.Id))?.SetValue(lead, integrationEvent.LeadId);

        var leads = new List<Lead> { lead };
        var leadSetMock = CreateMockDbSet(leads);

        UnitOfWorkMock
            .Setup(x => x.Set<Lead>())
            .Returns(leadSetMock.Object);

        UnitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        var wrapper = new IntegrationEventWrapper<LeadScoringFailedIntegrationEvent>(integrationEvent);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => 
            _sut.Handle(wrapper, CancellationToken.None));

        Assert.That(ex.Message, Is.EqualTo(exception.Message));

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains($"Error processing scoring failure for lead {integrationEvent.LeadId}")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_WhenLeadIsClosed_ShouldLogAndReturn(
        [WithValidLead(LeadStatus.Closed)] Lead lead,
        [WithLeadScoringFailedEvent] LeadScoringFailedIntegrationEvent integrationEvent)
    {
        LeadType.GetProperty(nameof(Lead.Id))?.SetValue(lead, integrationEvent.LeadId);

        var leads = new List<Lead> { lead };
        var leadSetMock = CreateMockDbSet(leads);

        UnitOfWorkMock
            .Setup(x => x.Set<Lead>())
            .Returns(leadSetMock.Object);

        var wrapper = new IntegrationEventWrapper<LeadScoringFailedIntegrationEvent>(integrationEvent);

        await _sut.Handle(wrapper, CancellationToken.None);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(
                    $"Lead {lead.Id} is already closed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test, AutoData]
    public async Task Handle_WhenLeadNotInInitialStatus_ShouldLogWarningAndReturn(
        [WithValidLead(LeadStatus.Qualified)] Lead lead,
        [WithLeadScoringFailedEvent] LeadScoringFailedIntegrationEvent integrationEvent)
    {
        LeadType.GetProperty(nameof(Lead.Id))?.SetValue(lead, integrationEvent.LeadId);

        var leads = new List<Lead> { lead };
        var leadSetMock = CreateMockDbSet(leads);

        UnitOfWorkMock
            .Setup(x => x.Set<Lead>())
            .Returns(leadSetMock.Object);

        var wrapper = new IntegrationEventWrapper<LeadScoringFailedIntegrationEvent>(integrationEvent);

        await _sut.Handle(wrapper, CancellationToken.None);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(
                    $"Cannot reject lead {lead.Id} from status {lead.Status}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        Assert.That(lead.Status, Is.EqualTo(LeadStatus.Qualified));
    }
}