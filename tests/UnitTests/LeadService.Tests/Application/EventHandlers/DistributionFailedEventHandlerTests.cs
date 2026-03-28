using AutoFixture.NUnit3;
using AvroSchemas.Messages.DistributionEvents;
using LeadService.Application.EventHandlers;
using LeadService.Domain.Entities;
using LeadService.Domain.Enums;
using LeadService.Tests.Common.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SharedTestInfrastructure.Database;

namespace LeadService.Tests.Application.EventHandlers;

/// <summary>
/// Тесты для DistributionFailedEventHandler
/// </summary>
[Category("Application")]
public class DistributionFailedEventHandlerTests : DatabaseTestBase
{
    private static readonly Type LeadType = typeof(Lead);

    private Mock<ILogger<DistributionFailedEventHandler>> _loggerMock = null!;
    private DistributionFailedEventHandler _sut = null!;

    [SetUp]
    public void Setup()
    {
        BaseSetup();
        _loggerMock = new Mock<ILogger<DistributionFailedEventHandler>>();
        _sut = new DistributionFailedEventHandler(UnitOfWorkMock.Object, _loggerMock.Object);
    }

    [TearDown]
    public void Cleanup()
    {
        BaseCleanup();
        _loggerMock.Reset();
    }

    [Test, AutoData]
    public async Task Handle_WhenLeadExists_ShouldMarkDistributionFailed(
        [WithValidLead(LeadStatus.Qualified)] Lead lead,
        [WithDistributionFailedEvent] DistributionFailed @event)
    {
        LeadType.GetProperty(nameof(Lead.Id))?.SetValue(lead, @event.LeadId);

        var leads = new List<Lead> { lead };
        var leadSetMock = CreateMockDbSet(leads);

        UnitOfWorkMock
            .Setup(x => x.Set<Lead>())
            .Returns(leadSetMock.Object);

        await _sut.Handle(@event, CancellationToken.None);

        Assert.That(lead.Status, Is.EqualTo(LeadStatus.FailedDistribution));
        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_WhenLeadNotFound_ShouldLogWarning(
        [WithDistributionFailedEvent] DistributionFailed @event)
    {
        var leads = new List<Lead>();
        var leadSetMock = CreateMockDbSet(leads);

        UnitOfWorkMock
            .Setup(x => x.Set<Lead>())
            .Returns(leadSetMock.Object);

        await _sut.Handle(@event, CancellationToken.None);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(
                    $"Lead not found: {@event.LeadId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test, AutoData]
    public void Handle_WhenConcurrencyException_ShouldThrow(
        [WithValidLead(LeadStatus.Qualified)] Lead lead,
        [WithDistributionFailedEvent] DistributionFailed @event)
    {
        LeadType.GetProperty(nameof(Lead.Id))?.SetValue(lead, @event.LeadId);

        var leads = new List<Lead> { lead };
        var leadSetMock = CreateMockDbSet(leads);

        UnitOfWorkMock
            .Setup(x => x.Set<Lead>())
            .Returns(leadSetMock.Object);

        UnitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => 
            _sut.Handle(@event, CancellationToken.None));

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains($"Concurrency conflict for lead {@event.LeadId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test, AutoData]
    public void Handle_WhenGenericException_ShouldThrow(
        [WithValidLead(LeadStatus.Qualified)] Lead lead,
        [WithDistributionFailedEvent] DistributionFailed @event,
        InvalidOperationException exception)
    {
        LeadType.GetProperty(nameof(Lead.Id))?.SetValue(lead, @event.LeadId);

        var leads = new List<Lead> { lead };
        var leadSetMock = CreateMockDbSet(leads);

        UnitOfWorkMock
            .Setup(x => x.Set<Lead>())
            .Returns(leadSetMock.Object);

        UnitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => 
            _sut.Handle(@event, CancellationToken.None));

        Assert.That(ex.Message, Is.EqualTo(exception.Message));

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains($"Error processing distribution failure for lead {@event.LeadId}")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_WhenLeadIsClosed_ShouldLogAndReturn(
        [WithValidLead(LeadStatus.Closed)] Lead lead,
        [WithDistributionFailedEvent] DistributionFailed @event)
    {
        LeadType.GetProperty(nameof(Lead.Id))?.SetValue(lead, @event.LeadId);

        var leads = new List<Lead> { lead };
        var leadSetMock = CreateMockDbSet(leads);

        UnitOfWorkMock
            .Setup(x => x.Set<Lead>())
            .Returns(leadSetMock.Object);

        await _sut.Handle(@event, CancellationToken.None);

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
    public async Task Handle_WhenLeadNotInQualifiedStatus_ShouldLogWarningAndReturn(
        [WithValidLead] Lead lead,
        [WithDistributionFailedEvent] DistributionFailed @event)
    {
        LeadType.GetProperty(nameof(Lead.Id))?.SetValue(lead, @event.LeadId);

        var leads = new List<Lead> { lead };
        var leadSetMock = CreateMockDbSet(leads);

        UnitOfWorkMock
            .Setup(x => x.Set<Lead>())
            .Returns(leadSetMock.Object);

        await _sut.Handle(@event, CancellationToken.None);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(
                    $"Cannot mark distribution failed for lead {lead.Id} from status {lead.Status}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        Assert.That(lead.Status, Is.EqualTo(LeadStatus.Initial));
    }
}