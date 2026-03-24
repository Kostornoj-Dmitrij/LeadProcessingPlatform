using AutoFixture.NUnit3;
using IntegrationEvents.LeadEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ScoringService.Application.EventHandlers;
using ScoringService.Domain.Entities;
using ScoringService.Tests.Common.Attributes;
using ScoringService.Tests.Common.Database;
using SharedKernel.Events;

namespace ScoringService.Tests.Application.EventHandlers;

/// <summary>
/// Тесты для LeadDistributionFailedEventHandler
/// </summary>
[Category("Application")]
public class LeadDistributionFailedEventHandlerTests : DatabaseTestBase
{
    private static readonly Type ResultType = typeof(ScoringResult);
    private static readonly Type RequestType = typeof(ScoringRequest);

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
    public async Task Handle_WhenScoringResultAndRequestExist_ShouldRemoveBothAndCreateLog(
        [WithValidLeadDistributionFailedEvent] LeadDistributionFailedIntegrationEvent integrationEvent,
        [WithValidScoringResult] ScoringResult scoringResult,
        [WithValidScoringRequest] ScoringRequest scoringRequest)
    {
        ResultType.GetProperty(nameof(ScoringResult.LeadId))?.SetValue(scoringResult, integrationEvent.LeadId);
        RequestType.GetProperty(nameof(ScoringRequest.LeadId))?.SetValue(scoringRequest, integrationEvent.LeadId);
        scoringRequest.UpdateEnrichedData("{\"data\":\"test\"}");

        var results = new List<ScoringResult> { scoringResult };
        var requests = new List<ScoringRequest> { scoringRequest };
        var logs = new List<CompensationLog>();

        var resultSetMock = CreateMockDbSet(results);
        var requestSetMock = CreateMockDbSet(requests);
        var logSetMock = CreateMockDbSet(logs);

        UnitOfWorkMock.Setup(x => x.Set<ScoringResult>()).Returns(resultSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<ScoringRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<CompensationLog>()).Returns(logSetMock.Object);

        var wrapper = new IntegrationEventWrapper<LeadDistributionFailedIntegrationEvent>(integrationEvent);

        await _sut.Handle(wrapper, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<CompensationLog>().AddAsync(
            It.IsAny<CompensationLog>(), It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWorkMock.Verify(x => x.Set<ScoringResult>().Remove(scoringResult), Times.Once);
        UnitOfWorkMock.Verify(x => x.Set<ScoringRequest>().Remove(scoringRequest), Times.Once);
        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(scoringRequest.EnrichedData, Is.Null);
    }

    [Test, AutoData]
    public async Task Handle_WhenOnlyRequestExists_ShouldRemoveRequestAndCreateLog(
        [WithValidLeadDistributionFailedEvent] LeadDistributionFailedIntegrationEvent integrationEvent,
        [WithValidScoringRequest] ScoringRequest scoringRequest,
        List<ScoringResult> results)
    {
        RequestType.GetProperty(nameof(ScoringRequest.LeadId))?.SetValue(scoringRequest, integrationEvent.LeadId);

        var requests = new List<ScoringRequest> { scoringRequest };
        var logs = new List<CompensationLog>();

        var resultSetMock = CreateMockDbSet(results);
        var requestSetMock = CreateMockDbSet(requests);
        var logSetMock = CreateMockDbSet(logs);

        UnitOfWorkMock.Setup(x => x.Set<ScoringResult>()).Returns(resultSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<ScoringRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<CompensationLog>()).Returns(logSetMock.Object);

        var wrapper = new IntegrationEventWrapper<LeadDistributionFailedIntegrationEvent>(integrationEvent);

        await _sut.Handle(wrapper, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<CompensationLog>().AddAsync(
            It.IsAny<CompensationLog>(), It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWorkMock.Verify(x => x.Set<ScoringRequest>().Remove(scoringRequest), Times.Once);
        UnitOfWorkMock.Verify(x => x.Set<ScoringResult>().Remove(It.IsAny<ScoringResult>()), Times.Never);
    }

    [Test, AutoData]
    public void Handle_WhenConcurrencyException_ShouldThrow(
        [WithValidLeadDistributionFailedEvent] LeadDistributionFailedIntegrationEvent integrationEvent,
        List<ScoringResult> results,
        List<ScoringRequest> requests,
        List<CompensationLog> logs)
    {
        var resultSetMock = CreateMockDbSet(results);
        var requestSetMock = CreateMockDbSet(requests);
        var logSetMock = CreateMockDbSet(logs);

        UnitOfWorkMock.Setup(x => x.Set<ScoringResult>()).Returns(resultSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<ScoringRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<CompensationLog>()).Returns(logSetMock.Object);
        UnitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        var wrapper = new IntegrationEventWrapper<LeadDistributionFailedIntegrationEvent>(integrationEvent);

        Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            _sut.Handle(wrapper, CancellationToken.None));
    }
}