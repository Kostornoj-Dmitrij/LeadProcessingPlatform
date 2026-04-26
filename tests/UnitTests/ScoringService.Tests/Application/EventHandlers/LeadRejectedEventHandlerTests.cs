using AutoFixture.NUnit4;
using AvroSchemas.Messages.LeadEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ScoringService.Application.EventHandlers;
using ScoringService.Domain.Entities;
using ScoringService.Tests.Common.Attributes;
using SharedTestInfrastructure.Database;

namespace ScoringService.Tests.Application.EventHandlers;

/// <summary>
/// Тесты для LeadRejectedEventHandler
/// </summary>
[Category("Application")]
public class LeadRejectedEventHandlerTests : DatabaseTestBase
{
    private static readonly Type ResultType = typeof(ScoringResult);
    private static readonly Type RequestType = typeof(ScoringRequest);

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
    public async Task Handle_WhenScoringResultAndRequestExist_ShouldRemoveBothAndCreateLog(
        [WithValidLeadRejectedEvent] LeadRejected @event,
        [WithValidScoringResult] ScoringResult scoringResult,
        [WithValidScoringRequest] ScoringRequest scoringRequest)
    {
        ResultType.GetProperty(nameof(ScoringResult.LeadId))?.SetValue(scoringResult, @event.LeadId);
        RequestType.GetProperty(nameof(ScoringRequest.LeadId))?.SetValue(scoringRequest, @event.LeadId);
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

        await _sut.Handle(@event, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<CompensationLog>().AddAsync(
            It.IsAny<CompensationLog>(), It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWorkMock.Verify(x => x.Set<ScoringResult>().Remove(scoringResult), Times.Once);
        UnitOfWorkMock.Verify(x => x.Set<ScoringRequest>().Remove(scoringRequest), Times.Once);
        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(scoringRequest.EnrichedData, Is.Null);
    }

    [Test, AutoData]
    public async Task Handle_WhenOnlyRequestExists_ShouldRemoveRequestAndCreateLog(
        [WithValidLeadRejectedEvent] LeadRejected @event,
        [WithValidScoringRequest] ScoringRequest scoringRequest,
        List<ScoringResult> results)
    {
        RequestType.GetProperty(nameof(ScoringRequest.LeadId))?.SetValue(scoringRequest, @event.LeadId);

        var requests = new List<ScoringRequest> { scoringRequest };
        var logs = new List<CompensationLog>();

        var resultSetMock = CreateMockDbSet(results);
        var requestSetMock = CreateMockDbSet(requests);
        var logSetMock = CreateMockDbSet(logs);

        UnitOfWorkMock.Setup(x => x.Set<ScoringResult>()).Returns(resultSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<ScoringRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<CompensationLog>()).Returns(logSetMock.Object);

        await _sut.Handle(@event, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<CompensationLog>().AddAsync(
            It.IsAny<CompensationLog>(), It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWorkMock.Verify(x => x.Set<ScoringRequest>().Remove(scoringRequest), Times.Once);
        UnitOfWorkMock.Verify(x => x.Set<ScoringResult>().Remove(It.IsAny<ScoringResult>()), Times.Never);
    }

    [Test, AutoData]
    public void Handle_WhenConcurrencyException_ShouldThrow(
        [WithValidLeadRejectedEvent] LeadRejected @event,
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

        Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            _sut.Handle(@event, CancellationToken.None));
    }
}