using AutoFixture.NUnit3;
using IntegrationEvents.ScoringEvents;
using LeadService.Application.EventHandlers;
using LeadService.Domain.Entities;
using LeadService.Tests.Common.Attributes;
using LeadService.Tests.Common.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SharedKernel.Events;

namespace LeadService.Tests.Application.EventHandlers;

/// <summary>
/// Тесты для LeadScoredEventHandler
/// </summary>
[Category("Application")]
public class LeadScoredEventHandlerTests : DatabaseTestBase
{
    private Mock<ILogger<LeadScoredEventHandler>> _loggerMock = null!;
    private LeadScoredEventHandler _sut = null!;

    [SetUp]
    public void Setup()
    {
        BaseSetup();
        _loggerMock = new Mock<ILogger<LeadScoredEventHandler>>();
        _sut = new LeadScoredEventHandler(UnitOfWorkMock.Object, _loggerMock.Object);
    }

    [TearDown]
    public void Cleanup()
    {
        BaseCleanup();
        _loggerMock.Reset();
    }

    [Test, AutoData]
    public async Task Handle_WhenLeadExists_ShouldMarkScoringReceived(
        [WithValidLead] Lead lead,
        [WithLeadScoredEvent] LeadScoredIntegrationEvent integrationEvent)
    {
        var leadType = typeof(Lead);
        leadType.GetProperty(nameof(Lead.Id))?.SetValue(lead, integrationEvent.LeadId);

        var leads = new List<Lead> { lead };
        var leadSetMock = CreateMockDbSet(leads);

        UnitOfWorkMock
            .Setup(x => x.Set<Lead>())
            .Returns(leadSetMock.Object);

        var wrapper = new IntegrationEventWrapper<LeadScoredIntegrationEvent>(integrationEvent);

        await _sut.Handle(wrapper, CancellationToken.None);

        Assert.That(lead.IsScoringReceived, Is.True);
        Assert.That(lead.Score, Is.EqualTo(integrationEvent.TotalScore));
        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_WhenLeadNotFound_ShouldLogWarning(
        [WithLeadScoredEvent] LeadScoredIntegrationEvent integrationEvent)
    {
        var leads = new List<Lead>();
        var leadSetMock = CreateMockDbSet(leads);

        UnitOfWorkMock
            .Setup(x => x.Set<Lead>())
            .Returns(leadSetMock.Object);

        var wrapper = new IntegrationEventWrapper<LeadScoredIntegrationEvent>(integrationEvent);

        await _sut.Handle(wrapper, CancellationToken.None);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains($"Lead not found for LeadId: {integrationEvent.LeadId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test, AutoData]
    public void Handle_WhenConcurrencyException_ShouldThrow(
        [WithValidLead] Lead lead,
        [WithLeadScoredEvent] LeadScoredIntegrationEvent integrationEvent)
    {
        var leadType = typeof(Lead);
        leadType.GetProperty(nameof(Lead.Id))?.SetValue(lead, integrationEvent.LeadId);

        var leads = new List<Lead> { lead };
        var leadSetMock = CreateMockDbSet(leads);

        UnitOfWorkMock
            .Setup(x => x.Set<Lead>())
            .Returns(leadSetMock.Object);

        UnitOfWorkMock
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        var wrapper = new IntegrationEventWrapper<LeadScoredIntegrationEvent>(integrationEvent);

        Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => 
            _sut.Handle(wrapper, CancellationToken.None));
    }

    [Test, AutoData]
    public void Handle_WhenGenericException_ShouldLogErrorAndThrow(
        [WithValidLead] Lead lead,
        [WithLeadScoredEvent] LeadScoredIntegrationEvent integrationEvent,
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

        var wrapper = new IntegrationEventWrapper<LeadScoredIntegrationEvent>(integrationEvent);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => 
            _sut.Handle(wrapper, CancellationToken.None));

        Assert.That(ex.Message, Is.EqualTo(exception.Message));

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains($"Error processing scoring for lead {integrationEvent.LeadId}")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}