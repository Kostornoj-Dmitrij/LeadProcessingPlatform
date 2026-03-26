using AutoFixture.NUnit3;
using EnrichmentService.Application.EventHandlers;
using EnrichmentService.Domain.Entities;
using EnrichmentService.Domain.Events;
using EnrichmentService.Tests.Common.Attributes;
using IntegrationEvents.LeadEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SharedKernel.Events;
using SharedTestInfrastructure.Database;

namespace EnrichmentService.Tests.Application.EventHandlers;

/// <summary>
/// Тесты для LeadRejectedEventHandler
/// </summary>
[Category("Application")]
public class LeadRejectedEventHandlerTests : DatabaseTestBase
{
    private static readonly Type ResultType = typeof(EnrichmentResult);

    private Mock<ILogger<LeadRejectedEventHandler>> _loggerMock = null!;
    private LeadRejectedEventHandler _sut = null!;

    [SetUp]
    public void Setup()
    {
        BaseSetup();
        _loggerMock = new Mock<ILogger<LeadRejectedEventHandler>>();
        _sut = new LeadRejectedEventHandler(UnitOfWorkMock.Object, _loggerMock.Object);
    }

    [TearDown]
    public void Cleanup()
    {
        BaseCleanup();
        _loggerMock.Reset();
    }

    [Test, AutoData]
    public async Task Handle_WhenEnrichmentExists_ShouldRemoveEnrichmentAndCreateCompensationLog(
        [WithValidLeadRejectedEvent] LeadRejectedIntegrationEvent integrationEvent,
        [WithValidEnrichmentResult] EnrichmentResult enrichmentResult,
        List<CompensationLog> logs)
    {
        ResultType.GetProperty(nameof(EnrichmentResult.LeadId))?.SetValue(enrichmentResult, integrationEvent.LeadId);

        var results = new List<EnrichmentResult> { enrichmentResult };
        var resultsSetMock = CreateMockDbSet(results);

        var logsSetMock = CreateMockDbSet(logs);

        UnitOfWorkMock
            .Setup(x => x.Set<EnrichmentResult>())
            .Returns(resultsSetMock.Object);

        UnitOfWorkMock
            .Setup(x => x.Set<CompensationLog>())
            .Returns(logsSetMock.Object);

        var wrapper = new IntegrationEventWrapper<LeadRejectedIntegrationEvent>(integrationEvent);

        await _sut.Handle(wrapper, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<CompensationLog>().AddAsync(
            It.IsAny<CompensationLog>(),
            It.IsAny<CancellationToken>()), Times.Once);

        UnitOfWorkMock.Verify(x => x.Set<EnrichmentResult>().Remove(enrichmentResult), Times.Once);
        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_WhenNoEnrichmentExists_ShouldOnlyCreateCompensationLog(
        [WithValidLeadRejectedEvent] LeadRejectedIntegrationEvent integrationEvent,
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

        var wrapper = new IntegrationEventWrapper<LeadRejectedIntegrationEvent>(integrationEvent);

        await _sut.Handle(wrapper, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<CompensationLog>().AddAsync(
            It.IsAny<CompensationLog>(),
            It.IsAny<CancellationToken>()), Times.Once);

        UnitOfWorkMock.Verify(x => x.Set<EnrichmentResult>().Remove(It.IsAny<EnrichmentResult>()), Times.Never);
        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_ShouldCreateCompensationLogWithCorrectData_WhenEnrichmentExists(
        [WithValidLeadRejectedEvent] LeadRejectedIntegrationEvent integrationEvent,
        [WithValidEnrichmentResult] EnrichmentResult enrichmentResult,
        List<CompensationLog> logs)
    {
        CompensationLog? createdLog = null;

        ResultType.GetProperty(nameof(EnrichmentResult.LeadId))?.SetValue(enrichmentResult, integrationEvent.LeadId);

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

        var wrapper = new IntegrationEventWrapper<LeadRejectedIntegrationEvent>(integrationEvent);

        await _sut.Handle(wrapper, CancellationToken.None);

        Assert.That(createdLog, Is.Not.Null);
        Assert.That(createdLog!.LeadId, Is.EqualTo(integrationEvent.LeadId));
        Assert.That(createdLog.Reason, Does.Contain("Enrichment data removed due to lead rejection"));
        Assert.That(createdLog.IsCompensated, Is.True);
        Assert.That(createdLog.ProcessedAt, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));

        Assert.That(createdLog.DomainEvents,
            Has.Exactly(1).InstanceOf<LeadEnrichmentCompensatedDomainEvent>());
    }

    [Test, AutoData]
    public async Task Handle_ShouldCreateCompensationLogWithCorrectData_WhenNoEnrichment(
        [WithValidLeadRejectedEvent] LeadRejectedIntegrationEvent integrationEvent,
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

        var wrapper = new IntegrationEventWrapper<LeadRejectedIntegrationEvent>(integrationEvent);

        await _sut.Handle(wrapper, CancellationToken.None);

        Assert.That(createdLog, Is.Not.Null);
        Assert.That(createdLog!.LeadId, Is.EqualTo(integrationEvent.LeadId));
        Assert.That(createdLog.Reason,
            Does.Contain("No enrichment data found, compensated anyway"));
        Assert.That(createdLog.IsCompensated, Is.True);
    }

    [Test, AutoData]
    public void Handle_WhenConcurrencyException_ShouldThrow(
        [WithValidLeadRejectedEvent] LeadRejectedIntegrationEvent integrationEvent,
        [WithValidEnrichmentResult] EnrichmentResult enrichmentResult,
        List<CompensationLog> logs)
    {
        ResultType.GetProperty(nameof(EnrichmentResult.LeadId))?.SetValue(enrichmentResult, integrationEvent.LeadId);

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

        var wrapper = new IntegrationEventWrapper<LeadRejectedIntegrationEvent>(integrationEvent);

        Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            _sut.Handle(wrapper, CancellationToken.None));
    }

    [Test, AutoData]
    public async Task Handle_ShouldLogInformation(
        [WithValidLeadRejectedEvent] LeadRejectedIntegrationEvent integrationEvent,
        [WithValidEnrichmentResult] EnrichmentResult enrichmentResult)
    {
        ResultType.GetProperty(nameof(EnrichmentResult.LeadId))?.SetValue(enrichmentResult, integrationEvent.LeadId);

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

        var wrapper = new IntegrationEventWrapper<LeadRejectedIntegrationEvent>(integrationEvent);

        await _sut.Handle(wrapper, CancellationToken.None);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(
                    $"Processing LeadRejected for lead {integrationEvent.LeadId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}