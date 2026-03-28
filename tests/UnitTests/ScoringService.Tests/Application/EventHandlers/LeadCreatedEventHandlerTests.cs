using AutoFixture.NUnit3;
using AvroSchemas.Messages.LeadEvents;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ScoringService.Application.EventHandlers;
using ScoringService.Domain.Entities;
using ScoringService.Tests.Common.Attributes;
using SharedTestInfrastructure.Database;

namespace ScoringService.Tests.Application.EventHandlers;

/// <summary>
/// Тесты для LeadCreatedEventHandler
/// </summary>
[Category("Application")]
public class LeadCreatedEventHandlerTests : DatabaseTestBase
{
    private static readonly Type RequestType = typeof(ScoringRequest);

    private Mock<ILogger<LeadCreatedEventHandler>> _loggerMock = null!;
    private LeadCreatedEventHandler _sut = null!;

    [SetUp]
    public void Setup()
    {
        BaseSetup();
        _loggerMock = new Mock<ILogger<LeadCreatedEventHandler>>();
        _sut = new LeadCreatedEventHandler(UnitOfWorkMock.Object, _loggerMock.Object);
    }

    [TearDown]
    public void Cleanup()
    {
        BaseCleanup();
        _loggerMock.Reset();
    }

    [Test, AutoData]
    public async Task Handle_WhenNoExistingRequestAndNoPendingData_ShouldCreateRequest(
        [WithValidLeadCreatedEvent] LeadCreated @event,
        List<ScoringRequest> requests,
        List<PendingEnrichedData> pendingData)
    {
        var requestSetMock = CreateMockDbSet(requests);
        var pendingSetMock = CreateMockDbSet(pendingData);

        UnitOfWorkMock.Setup(x => x.Set<ScoringRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<PendingEnrichedData>()).Returns(pendingSetMock.Object);

        await _sut.Handle(@event, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<ScoringRequest>().AddAsync(
            It.IsAny<ScoringRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_WhenPendingDataExists_ShouldUseItAndMarkProcessed(
        [WithValidLeadCreatedEvent] LeadCreated @event,
        [WithValidPendingEnrichedData] PendingEnrichedData pendingData,
        List<ScoringRequest> requests)
    {
        pendingData = PendingEnrichedData.Create(@event.LeadId, "{\"industry\":\"Tech\"}");

        var requestSetMock = CreateMockDbSet(requests);
        var pendingSetMock = CreateMockDbSet([pendingData]);

        UnitOfWorkMock.Setup(x => x.Set<ScoringRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<PendingEnrichedData>()).Returns(pendingSetMock.Object);

        ScoringRequest? createdRequest = null;
        UnitOfWorkMock.Setup(x => x.Set<ScoringRequest>().AddAsync(
                It.IsAny<ScoringRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ScoringRequest, CancellationToken>((req, _) => createdRequest = req)
            .ReturnsAsync((ScoringRequest _, CancellationToken _) => null!);

        await _sut.Handle(@event, CancellationToken.None);

        Assert.That(createdRequest, Is.Not.Null);
        Assert.That(createdRequest!.EnrichedData, Is.Not.Null);
        Assert.That(pendingData.IsProcessed, Is.True);
        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_WhenExistingRequestExists_ShouldSkip(
        [WithValidLeadCreatedEvent] LeadCreated @event,
        [WithValidScoringRequest] ScoringRequest existingRequest)
    {
        RequestType.GetProperty(nameof(ScoringRequest.LeadId))?.SetValue(existingRequest, @event.LeadId);

        var requests = new List<ScoringRequest> { existingRequest };
        var requestSetMock = CreateMockDbSet(requests);
        var pendingSetMock = CreateMockDbSet(new List<PendingEnrichedData>());

        UnitOfWorkMock.Setup(x => x.Set<ScoringRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<PendingEnrichedData>()).Returns(pendingSetMock.Object);

        await _sut.Handle(@event, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<ScoringRequest>().AddAsync(
            It.IsAny<ScoringRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}