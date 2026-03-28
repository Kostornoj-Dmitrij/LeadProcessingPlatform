using AutoFixture.NUnit3;
using AvroSchemas.Messages.EnrichmentEvents;
using AvroSchemas.Messages.ScoringEvents;
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
/// Тесты для LeadQualificationHandler
/// </summary>
[Category("Application")]
public class LeadQualificationHandlerTests : DatabaseTestBase
{
    private static readonly Type LeadType = typeof(Lead);

    private Mock<ILogger<LeadQualificationHandler>> _loggerMock = null!;
    private TestableLeadQualificationHandler _sut = null!;

    [SetUp]
    public void Setup()
    {
        BaseSetup();
        _loggerMock = new Mock<ILogger<LeadQualificationHandler>>();
    }

    [TearDown]
    public void Cleanup()
    {
        BaseCleanup();
        _loggerMock.Reset();
    }

    private void SetupSut(Func<Guid, CancellationToken, Task<Lead?>> getLeadFunc)
    {
        _sut = new TestableLeadQualificationHandler(UnitOfWorkMock.Object, _loggerMock.Object, getLeadFunc);
    }

    [Test, AutoData]
    public async Task HandleLeadEnriched_WhenLeadExists_ShouldMarkEnrichmentReceived(
        [WithValidLead] Lead lead,
        [WithLeadEnrichedEvent] LeadEnriched @event)
    {
        LeadType.GetProperty(nameof(Lead.Id))?.SetValue(lead, @event.LeadId);

        SetupSut((_, _) => Task.FromResult<Lead?>(lead));

        UnitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));

        await _sut.Handle(@event, CancellationToken.None);

        Assert.That(lead.IsEnrichmentReceived, Is.True);
        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test, AutoData]
    public async Task HandleLeadEnriched_WhenBothEventsReceived_ShouldQualify(
        [WithValidLead] Lead lead,
        [WithLeadEnrichedEvent] LeadEnriched enrichedEvent,
        [WithLeadScoredEvent] LeadScored scoredEvent)
    {
        LeadType.GetProperty(nameof(Lead.Id))?.SetValue(lead, enrichedEvent.LeadId);
        scoredEvent.LeadId = enrichedEvent.LeadId;

        SetupSut((_, _) => Task.FromResult<Lead?>(lead));

        UnitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));


        await _sut.Handle(enrichedEvent, CancellationToken.None);
        await _sut.Handle(scoredEvent, CancellationToken.None);

        Assert.That(lead.Status, Is.EqualTo(LeadStatus.Qualified));
        Assert.That(lead.Score, Is.EqualTo(scoredEvent.TotalScore));
        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Test, AutoData]
    public async Task HandleLeadEnriched_WhenLeadNotFound_ShouldLogWarning(
        [WithLeadEnrichedEvent] LeadEnriched @event)
    {
        SetupSut((_, _) => Task.FromResult<Lead?>(null));

        await _sut.Handle(@event, CancellationToken.None);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(
                    $"Lead {@event.LeadId} not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test, AutoData]
    public async Task HandleLeadEnriched_WhenLeadClosed_ShouldLogAndReturn(
        [WithValidLead(LeadStatus.Closed)] Lead lead,
        [WithLeadEnrichedEvent] LeadEnriched @event)
    {
        LeadType.GetProperty(nameof(Lead.Id))?.SetValue(lead, @event.LeadId);

        SetupSut((_, _) => Task.FromResult<Lead?>(lead));

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

        Assert.That(lead.IsEnrichmentReceived, Is.False);
        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test, AutoData]
    public async Task HandleLeadEnriched_WhenAlreadyReceived_ShouldNotApplyAgain(
        [WithValidLead] Lead lead,
        [WithLeadEnrichedEvent] LeadEnriched firstEvent,
        [WithLeadEnrichedEvent] LeadEnriched secondEvent)
    {
        LeadType.GetProperty(nameof(Lead.Id))?.SetValue(lead, firstEvent.LeadId);
        secondEvent.LeadId = firstEvent.LeadId;

        SetupSut((_, _) => Task.FromResult<Lead?>(lead));

        UnitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));

        await _sut.Handle(firstEvent, CancellationToken.None);
        var firstEnrichedData = lead.EnrichedData;

        await _sut.Handle(secondEvent, CancellationToken.None);

        Assert.That(lead.EnrichedData, Is.EqualTo(firstEnrichedData));
        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(1));
    }

    [Test, AutoData]
    public async Task HandleLeadScored_WhenLeadExists_ShouldMarkScoringReceived(
        [WithValidLead] Lead lead,
        [WithLeadScoredEvent] LeadScored @event)
    {
        LeadType.GetProperty(nameof(Lead.Id))?.SetValue(lead, @event.LeadId);

        SetupSut((_, _) => Task.FromResult<Lead?>(lead));

        UnitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));

        await _sut.Handle(@event, CancellationToken.None);

        Assert.That(lead.IsScoringReceived, Is.True);
        Assert.That(lead.Score, Is.EqualTo(@event.TotalScore));
        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test, AutoData]
    public async Task HandleLeadScored_WhenBothEventsReceived_ShouldQualify(
        [WithValidLead] Lead lead,
        [WithLeadScoredEvent] LeadScored scoredEvent,
        [WithLeadEnrichedEvent] LeadEnriched enrichedEvent)
    {
        LeadType.GetProperty(nameof(Lead.Id))?.SetValue(lead, scoredEvent.LeadId);
        enrichedEvent.LeadId = scoredEvent.LeadId;

        SetupSut((_, _) => Task.FromResult<Lead?>(lead));

        UnitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));

        await _sut.Handle(scoredEvent, CancellationToken.None);
        await _sut.Handle(enrichedEvent, CancellationToken.None);

        Assert.That(lead.Status, Is.EqualTo(LeadStatus.Qualified));
        Assert.That(lead.Score, Is.EqualTo(scoredEvent.TotalScore));
    }

    [Test, AutoData]
    public async Task HandleLeadScored_WhenLeadClosed_ShouldLogAndReturn(
        [WithValidLead(LeadStatus.Closed)] Lead lead,
        [WithLeadScoredEvent] LeadScored @event)
    {
        LeadType.GetProperty(nameof(Lead.Id))?.SetValue(lead, @event.LeadId);

        SetupSut((_, _) => Task.FromResult<Lead?>(lead));

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

        Assert.That(lead.IsScoringReceived, Is.False);
        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test, AutoData]
    public async Task HandleLeadScored_WhenAlreadyReceived_ShouldNotApplyAgain(
        [WithValidLead] Lead lead,
        [WithLeadScoredEvent] LeadScored firstEvent,
        [WithLeadScoredEvent] LeadScored secondEvent)
    {
        LeadType.GetProperty(nameof(Lead.Id))?.SetValue(lead, firstEvent.LeadId);
        secondEvent.LeadId = firstEvent.LeadId;

        SetupSut((_, _) => Task.FromResult<Lead?>(lead));

        UnitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));

        await _sut.Handle(firstEvent, CancellationToken.None);
        var firstScore = lead.Score;
        
        await _sut.Handle(secondEvent, CancellationToken.None);

        Assert.That(lead.Score, Is.EqualTo(firstScore));
        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(1));
    }

    [Test, AutoData]
    public void Handle_WhenConcurrencyException_ShouldThrow(
        [WithValidLead] Lead lead,
        [WithLeadEnrichedEvent] LeadEnriched @event)
    {
        LeadType.GetProperty(nameof(Lead.Id))?.SetValue(lead, @event.LeadId);

        SetupSut((_, _) => Task.FromResult<Lead?>(lead));

        UnitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => 
            _sut.Handle(@event, CancellationToken.None));

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains($"Concurrency conflict processing event for lead {@event.LeadId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test, AutoData]
    public void Handle_WhenGenericException_ShouldThrow(
        [WithValidLead] Lead lead,
        [WithLeadEnrichedEvent] LeadEnriched @event,
        InvalidOperationException exception)
    {
        LeadType.GetProperty(nameof(Lead.Id))?.SetValue(lead, @event.LeadId);

        SetupSut((_, _) => Task.FromResult<Lead?>(lead));

        UnitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => 
            _sut.Handle(@event, CancellationToken.None));

        Assert.That(ex.Message, Is.EqualTo(exception.Message));

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains($"Error processing event for lead {@event.LeadId}")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test, AutoData]
    public async Task HandleLeadEnriched_WhenLeadNotInInitialStatus_ShouldLogWarningAndReturn(
        [WithValidLead(LeadStatus.Qualified)] Lead lead,
        [WithLeadEnrichedEvent] LeadEnriched @event)
    {
        LeadType.GetProperty(nameof(Lead.Id))?.SetValue(lead, @event.LeadId);
        LeadType.GetProperty(nameof(Lead.IsEnrichmentReceived))?.SetValue(lead, false);
        LeadType.GetProperty(nameof(Lead.IsScoringReceived))?.SetValue(lead, true);

        SetupSut((_, _) => Task.FromResult<Lead?>(lead));

        await _sut.Handle(@event, CancellationToken.None);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(
                    $"Cannot process event for lead {lead.Id} in status {lead.Status}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        Assert.That(lead.IsEnrichmentReceived, Is.False);
        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        UnitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test, AutoData]
    public async Task HandleLeadScored_WhenLeadNotInInitialStatus_ShouldLogWarningAndReturn(
        [WithValidLead(LeadStatus.Qualified)] Lead lead,
        [WithLeadScoredEvent] LeadScored @event)
    {
        LeadType.GetProperty(nameof(Lead.Id))?.SetValue(lead, @event.LeadId);
        LeadType.GetProperty(nameof(Lead.IsEnrichmentReceived))?.SetValue(lead, true);
        LeadType.GetProperty(nameof(Lead.IsScoringReceived))?.SetValue(lead, false);

        SetupSut((_, _) => Task.FromResult<Lead?>(lead));

        await _sut.Handle(@event, CancellationToken.None);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(
                    $"Cannot process event for lead {lead.Id} in status {lead.Status}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        Assert.That(lead.IsScoringReceived, Is.False);
        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        UnitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}