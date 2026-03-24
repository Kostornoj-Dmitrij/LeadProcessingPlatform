using AutoFixture.NUnit3;
using IntegrationEvents.EnrichmentEvents;
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
/// Тесты для LeadEnrichedEventHandler
/// </summary>
[Category("Application")]
public class LeadEnrichedEventHandlerTests : DatabaseTestBase
{
    private static readonly Type RequestType = typeof(ScoringRequest);

    private Mock<ILogger<LeadEnrichedEventHandler>> _loggerMock = null!;
    private LeadEnrichedEventHandler _sut = null!;

    [SetUp]
    public void Setup()
    {
        BaseSetup();
        _loggerMock = new Mock<ILogger<LeadEnrichedEventHandler>>();
        _sut = new LeadEnrichedEventHandler(UnitOfWorkMock.Object, _loggerMock.Object);
    }

    [TearDown]
    public void Cleanup()
    {
        BaseCleanup();
        _loggerMock.Reset();
    }

    [Test, AutoData]
    public async Task Handle_WhenScoringRequestExistsAndNotCompleted_ShouldUpdateEnrichedData(
        [WithValidLeadEnrichedEvent] LeadEnrichedIntegrationEvent integrationEvent,
        [WithValidScoringRequest] ScoringRequest scoringRequest,
        List<PendingEnrichedData> pendingData)
    {
        RequestType.GetProperty(nameof(ScoringRequest.LeadId))?.SetValue(scoringRequest, integrationEvent.LeadId);

        var requests = new List<ScoringRequest> { scoringRequest };
        var pendingSetMock = CreateMockDbSet(pendingData);
        var requestSetMock = CreateMockDbSet(requests);

        UnitOfWorkMock.Setup(x => x.Set<ScoringRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<PendingEnrichedData>()).Returns(pendingSetMock.Object);

        var wrapper = new IntegrationEventWrapper<LeadEnrichedIntegrationEvent>(integrationEvent);

        await _sut.Handle(wrapper, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(scoringRequest.EnrichedData, Is.Not.Null);
        Assert.That(scoringRequest.EnrichedData, Does.Contain(integrationEvent.Industry));
    }

    [Test, AutoData]
    public async Task Handle_WhenScoringRequestExistsButCompleted_ShouldIgnore(
        [WithValidLeadEnrichedEvent] LeadEnrichedIntegrationEvent integrationEvent,
        [WithValidScoringRequest] ScoringRequest scoringRequest)
    {
        RequestType.GetProperty(nameof(ScoringRequest.LeadId))?.SetValue(scoringRequest, integrationEvent.LeadId);
        scoringRequest.StartProcessing();
        scoringRequest.MarkCompleted(75, 50, ["rule"]);

        var requests = new List<ScoringRequest> { scoringRequest };
        var requestSetMock = CreateMockDbSet(requests);
        var pendingSetMock = CreateMockDbSet(new List<PendingEnrichedData>());

        UnitOfWorkMock.Setup(x => x.Set<ScoringRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<PendingEnrichedData>()).Returns(pendingSetMock.Object);

        var wrapper = new IntegrationEventWrapper<LeadEnrichedIntegrationEvent>(integrationEvent);

        await _sut.Handle(wrapper, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test, AutoData]
    public async Task Handle_WhenNoScoringRequestExists_ShouldStoreAsPending(
        [WithValidLeadEnrichedEvent] LeadEnrichedIntegrationEvent integrationEvent,
        List<ScoringRequest> requests)
    {
        var requestSetMock = CreateMockDbSet(requests);
        var pendingSetMock = CreateMockDbSet(new List<PendingEnrichedData>());

        UnitOfWorkMock.Setup(x => x.Set<ScoringRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<PendingEnrichedData>()).Returns(pendingSetMock.Object);

        var wrapper = new IntegrationEventWrapper<LeadEnrichedIntegrationEvent>(integrationEvent);

        await _sut.Handle(wrapper, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<PendingEnrichedData>().AddAsync(
            It.IsAny<PendingEnrichedData>(), It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}