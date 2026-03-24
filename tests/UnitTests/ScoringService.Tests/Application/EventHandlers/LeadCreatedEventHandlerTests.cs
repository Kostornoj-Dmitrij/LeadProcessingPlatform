using AutoFixture.NUnit3;
using IntegrationEvents.LeadEvents;
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
        [WithValidLeadCreatedEvent] LeadCreatedIntegrationEvent integrationEvent,
        List<ScoringRequest> requests,
        List<PendingEnrichedData> pendingData)
    {
        var requestSetMock = CreateMockDbSet(requests);
        var pendingSetMock = CreateMockDbSet(pendingData);

        UnitOfWorkMock.Setup(x => x.Set<ScoringRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<PendingEnrichedData>()).Returns(pendingSetMock.Object);

        var wrapper = new IntegrationEventWrapper<LeadCreatedIntegrationEvent>(integrationEvent);

        await _sut.Handle(wrapper, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<ScoringRequest>().AddAsync(
            It.IsAny<ScoringRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_WhenPendingDataExists_ShouldUseItAndMarkProcessed(
        [WithValidLeadCreatedEvent] LeadCreatedIntegrationEvent integrationEvent,
        [WithValidPendingEnrichedData] PendingEnrichedData pendingData,
        List<ScoringRequest> requests)
    {
        pendingData = PendingEnrichedData.Create(integrationEvent.LeadId, "{\"industry\":\"Tech\"}");

        var requestSetMock = CreateMockDbSet(requests);
        var pendingSetMock = CreateMockDbSet([pendingData]);

        UnitOfWorkMock.Setup(x => x.Set<ScoringRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<PendingEnrichedData>()).Returns(pendingSetMock.Object);

        ScoringRequest? createdRequest = null;
        UnitOfWorkMock.Setup(x => x.Set<ScoringRequest>().AddAsync(
                It.IsAny<ScoringRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ScoringRequest, CancellationToken>((req, _) => createdRequest = req)
            .ReturnsAsync((ScoringRequest _, CancellationToken _) => null!);

        var wrapper = new IntegrationEventWrapper<LeadCreatedIntegrationEvent>(integrationEvent);

        await _sut.Handle(wrapper, CancellationToken.None);

        Assert.That(createdRequest, Is.Not.Null);
        Assert.That(createdRequest!.EnrichedData, Is.Not.Null);
        Assert.That(pendingData.IsProcessed, Is.True);
        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_WhenExistingRequestExists_ShouldSkip(
        [WithValidLeadCreatedEvent] LeadCreatedIntegrationEvent integrationEvent,
        [WithValidScoringRequest] ScoringRequest existingRequest)
    {
        RequestType.GetProperty(nameof(ScoringRequest.LeadId))?.SetValue(existingRequest, integrationEvent.LeadId);

        var requests = new List<ScoringRequest> { existingRequest };
        var requestSetMock = CreateMockDbSet(requests);
        var pendingSetMock = CreateMockDbSet(new List<PendingEnrichedData>());

        UnitOfWorkMock.Setup(x => x.Set<ScoringRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<PendingEnrichedData>()).Returns(pendingSetMock.Object);

        var wrapper = new IntegrationEventWrapper<LeadCreatedIntegrationEvent>(integrationEvent);

        await _sut.Handle(wrapper, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<ScoringRequest>().AddAsync(
            It.IsAny<ScoringRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}