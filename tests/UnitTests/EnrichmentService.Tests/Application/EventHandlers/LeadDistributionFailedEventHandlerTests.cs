using AutoFixture.NUnit4;
using AvroSchemas.Messages.LeadEvents;
using EnrichmentService.Application.EventHandlers;
using EnrichmentService.Domain.Entities;
using EnrichmentService.Domain.Events;
using EnrichmentService.Tests.Common.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SharedTestInfrastructure.Database;

namespace EnrichmentService.Tests.Application.EventHandlers;

/// <summary>
/// Тесты для LeadDistributionFailedEventHandler
/// </summary>
[Category("Application")]
public class LeadDistributionFailedEventHandlerTests : DatabaseTestBase
{
    private static readonly Type ResultType = typeof(EnrichmentResult);

    private Mock<ILogger<LeadDistributionFailedEventHandler>> _loggerMock = null!;
    private LeadDistributionFailedEventHandler _sut = null!;

    [SetUp]
    public void Setup()
    {
        BaseSetup();
        _loggerMock = new Mock<ILogger<LeadDistributionFailedEventHandler>>();
        _sut = new LeadDistributionFailedEventHandler(UnitOfWorkMock.Object, _loggerMock.Object);
    }

    [TearDown]
    public void Cleanup()
    {
        BaseCleanup();
        _loggerMock.Reset();
    }

    [Test, AutoData]
    public async Task Handle_WhenEnrichmentExists_ShouldRemoveEnrichmentAndCreateCompensationLog(
        [WithValidLeadDistributionFailedEvent] LeadDistributionFailed @event,
        [WithValidEnrichmentResult] EnrichmentResult enrichmentResult)
    {
        ResultType.GetProperty(nameof(EnrichmentResult.LeadId))?.SetValue(enrichmentResult, @event.LeadId);

        var results = new List<EnrichmentResult> { enrichmentResult };
        var resultsSetMock = CreateMockDbSet(results);

        var logs = new List<CompensationLog>();
        var logsSetMock = CreateMockDbSet(logs);

        UnitOfWorkMock
            .Setup(x => x.Set<EnrichmentResult>())
            .Returns(resultsSetMock.Object);

        UnitOfWorkMock
            .Setup(x => x.Set<CompensationLog>())
            .Returns(logsSetMock.Object);

        await _sut.Handle(@event, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<CompensationLog>().AddAsync(
            It.IsAny<CompensationLog>(),
            It.IsAny<CancellationToken>()), Times.Once);

        UnitOfWorkMock.Verify(x =>
            x.Set<EnrichmentResult>().Remove(enrichmentResult), Times.Once);
        UnitOfWorkMock.Verify(x =>
            x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_WhenNoEnrichmentExists_ShouldOnlyCreateCompensationLog(
        [WithValidLeadDistributionFailedEvent] LeadDistributionFailed @event,
        List<EnrichmentResult> results,
        List<CompensationLog> logs)
    {
        var resultsSetMock = CreateMockDbSet(results);
        var logsSetMock = CreateMockDbSet(logs);

        UnitOfWorkMock
            .Setup(x => x.Set<EnrichmentResult>())
            .Returns(resultsSetMock.Object);

        UnitOfWorkMock
            .Setup(x => x.Set<CompensationLog>())
            .Returns(logsSetMock.Object);

        await _sut.Handle(@event, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<CompensationLog>().AddAsync(
            It.IsAny<CompensationLog>(),
            It.IsAny<CancellationToken>()), Times.Once);

        UnitOfWorkMock.Verify(x =>
            x.Set<EnrichmentResult>().Remove(It.IsAny<EnrichmentResult>()), Times.Never);
        UnitOfWorkMock.Verify(x =>
            x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_ShouldCreateCompensationLogWithCorrectData_WhenEnrichmentExists(
        [WithValidLeadDistributionFailedEvent] LeadDistributionFailed @event,
        [WithValidEnrichmentResult] EnrichmentResult enrichmentResult,
        List<CompensationLog> logs)
    {
        CompensationLog? createdLog = null;

        ResultType.GetProperty(nameof(EnrichmentResult.LeadId))?.SetValue(enrichmentResult, @event.LeadId);

        var results = new List<EnrichmentResult> { enrichmentResult };
        var resultsSetMock = CreateMockDbSet(results);

        var logsSetMock = CreateMockDbSet(logs);

        UnitOfWorkMock
            .Setup(x => x.Set<EnrichmentResult>())
            .Returns(resultsSetMock.Object);

        UnitOfWorkMock
            .Setup(x => x.Set<CompensationLog>())
            .Returns(logsSetMock.Object);

        UnitOfWorkMock
            .Setup(x => x.Set<CompensationLog>().AddAsync(
                It.IsAny<CompensationLog>(),
                It.IsAny<CancellationToken>()))
            .Callback<CompensationLog, CancellationToken>((log, _) => createdLog = log)
            .ReturnsAsync((CompensationLog _, CancellationToken _) => null!);

        await _sut.Handle(@event, CancellationToken.None);

        Assert.That(createdLog, Is.Not.Null);
        Assert.That(createdLog!.LeadId, Is.EqualTo(@event.LeadId));
        Assert.That(createdLog.Reason, Does.Contain("Enrichment data removed"));
        Assert.That(createdLog.IsCompensated, Is.True);
        Assert.That(createdLog.ProcessedAt, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));

        Assert.That(createdLog.DomainEvents, Has.Exactly(1)
            .InstanceOf<LeadEnrichmentCompensatedDomainEvent>());
    }

    [Test, AutoData]
    public async Task Handle_ShouldCreateCompensationLogWithCorrectData_WhenNoEnrichment(
        [WithValidLeadDistributionFailedEvent] LeadDistributionFailed @event,
        List<EnrichmentResult> results,
        List<CompensationLog> logs)
    {
        CompensationLog? createdLog = null;

        var resultsSetMock = CreateMockDbSet(results);
        var logsSetMock = CreateMockDbSet(logs);

        UnitOfWorkMock
            .Setup(x => x.Set<EnrichmentResult>())
            .Returns(resultsSetMock.Object);

        UnitOfWorkMock
            .Setup(x => x.Set<CompensationLog>())
            .Returns(logsSetMock.Object);

        UnitOfWorkMock
            .Setup(x => x.Set<CompensationLog>().AddAsync(
                It.IsAny<CompensationLog>(),
                It.IsAny<CancellationToken>()))
            .Callback<CompensationLog, CancellationToken>((log, _) => createdLog = log)
            .ReturnsAsync((CompensationLog _, CancellationToken _) => null!);

        await _sut.Handle(@event, CancellationToken.None);

        Assert.That(createdLog, Is.Not.Null);
        Assert.That(createdLog!.LeadId, Is.EqualTo(@event.LeadId));
        Assert.That(createdLog.Reason, Does.Contain("No enrichment data found, compensated anyway"));
        Assert.That(createdLog.IsCompensated, Is.True);
    }

    [Test, AutoData]
    public void Handle_WhenConcurrencyException_ShouldThrow(
        [WithValidLeadDistributionFailedEvent] LeadDistributionFailed @event,
        [WithValidEnrichmentResult] EnrichmentResult enrichmentResult,
        List<CompensationLog> logs)
    {
        ResultType.GetProperty(nameof(EnrichmentResult.LeadId))?.SetValue(enrichmentResult, @event.LeadId);

        var results = new List<EnrichmentResult> { enrichmentResult };
        var resultsSetMock = CreateMockDbSet(results);

        var logsSetMock = CreateMockDbSet(logs);

        UnitOfWorkMock
            .Setup(x => x.Set<EnrichmentResult>())
            .Returns(resultsSetMock.Object);

        UnitOfWorkMock
            .Setup(x => x.Set<CompensationLog>())
            .Returns(logsSetMock.Object);

        UnitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            _sut.Handle(@event, CancellationToken.None));
    }
}