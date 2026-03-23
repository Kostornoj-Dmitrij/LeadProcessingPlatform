using AutoFixture.NUnit3;
using IntegrationEvents.ScoringEvents;
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
/// Тесты для LeadScoringCompensatedEventHandler
/// </summary>
[Category("Application")]
public class LeadScoringCompensatedEventHandlerTests : DatabaseTestBase
{
    private static readonly Type LeadType = typeof(Lead);

    private Mock<ILogger<LeadScoringCompensatedEventHandler>> _loggerMock = null!;
    private LeadScoringCompensatedEventHandler _sut = null!;

    [SetUp]
    public void Setup()
    {
        BaseSetup();
        _loggerMock = new Mock<ILogger<LeadScoringCompensatedEventHandler>>();
        _sut = new LeadScoringCompensatedEventHandler(UnitOfWorkMock.Object, _loggerMock.Object);
    }

    [TearDown]
    public void Cleanup()
    {
        BaseCleanup();
        _loggerMock.Reset();
    }

    [Test, AutoData]
    public async Task Handle_WhenLeadExists_ShouldMarkScoringCompensated(
        [WithValidLead(LeadStatus.Rejected)] Lead lead,
        [WithLeadScoringCompensatedEvent] LeadScoringCompensatedIntegrationEvent integrationEvent)
    {
        LeadType.GetProperty(nameof(Lead.Id))?.SetValue(lead, integrationEvent.LeadId);

        var leads = new List<Lead> { lead };
        var leadSetMock = CreateMockDbSet(leads);

        UnitOfWorkMock
            .Setup(x => x.Set<Lead>())
            .Returns(leadSetMock.Object);

        var wrapper = new IntegrationEventWrapper<LeadScoringCompensatedIntegrationEvent>(integrationEvent);

        await _sut.Handle(wrapper, CancellationToken.None);

        Assert.That(lead.IsScoringCompensated, Is.True);
        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_WhenLeadNotFound_ShouldLogWarning(
        [WithLeadScoringCompensatedEvent] LeadScoringCompensatedIntegrationEvent integrationEvent)
    {
        var leads = new List<Lead>();
        var leadSetMock = CreateMockDbSet(leads);

        UnitOfWorkMock
            .Setup(x => x.Set<Lead>())
            .Returns(leadSetMock.Object);

        var wrapper = new IntegrationEventWrapper<LeadScoringCompensatedIntegrationEvent>(integrationEvent);

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
        [WithValidLead(LeadStatus.Rejected)] Lead lead,
        [WithLeadScoringCompensatedEvent] LeadScoringCompensatedIntegrationEvent integrationEvent)
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

        var wrapper = new IntegrationEventWrapper<LeadScoringCompensatedIntegrationEvent>(integrationEvent);

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
        [WithValidLead(LeadStatus.Rejected)] Lead lead,
        [WithLeadScoringCompensatedEvent] LeadScoringCompensatedIntegrationEvent integrationEvent,
        InvalidOperationException exception)
    {
        LeadType.GetProperty(nameof(Lead.Id))?.SetValue(lead, integrationEvent.LeadId);

        var leads = new List<Lead> { lead };
        var leadSetMock = CreateMockDbSet(leads);

        UnitOfWorkMock
            .Setup(x => x.Set<Lead>())
            .Returns(leadSetMock.Object);

        UnitOfWorkMock
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        var wrapper = new IntegrationEventWrapper<LeadScoringCompensatedIntegrationEvent>(integrationEvent);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => 
            _sut.Handle(wrapper, CancellationToken.None));

        Assert.That(ex.Message, Is.EqualTo(exception.Message));

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains($"Error processing scoring compensation for lead {integrationEvent.LeadId}")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}