using AutoFixture.NUnit3;
using EnrichmentService.Application.EventHandlers;
using EnrichmentService.Domain.Entities;
using EnrichmentService.Domain.Enums;
using EnrichmentService.Tests.Common.Attributes;
using IntegrationEvents.LeadEvents;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SharedKernel.Events;
using SharedTestInfrastructure.Database;

namespace EnrichmentService.Tests.Application.EventHandlers;

/// <summary>
/// Тесты для LeadCreatedEventHandler
/// </summary>
[Category("Application")]
public class LeadCreatedEventHandlerTests : DatabaseTestBase
{
    private static readonly Type RequestType = typeof(EnrichmentRequest);

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
    public async Task Handle_WhenNoExistingRequest_ShouldCreateEnrichmentRequest(
        [WithValidLeadCreatedEvent] LeadCreatedIntegrationEvent integrationEvent,
        List<EnrichmentRequest> requests)
    {
        var requestSetMock = CreateMockDbSet(requests);

        UnitOfWorkMock
            .Setup(x => x.Set<EnrichmentRequest>())
            .Returns(requestSetMock.Object);

        var wrapper = new IntegrationEventWrapper<LeadCreatedIntegrationEvent>(integrationEvent);

        await _sut.Handle(wrapper, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<EnrichmentRequest>().AddAsync(
            It.IsAny<EnrichmentRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);

        UnitOfWorkMock.Verify(x =>
            x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(
                    $"Enrichment request created for lead {integrationEvent.LeadId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_WhenExistingRequestExists_ShouldSkip(
        [WithValidLeadCreatedEvent] LeadCreatedIntegrationEvent integrationEvent,
        [WithValidEnrichmentRequest] EnrichmentRequest existingRequest)
    {
        RequestType.GetProperty(nameof(EnrichmentRequest.LeadId))?.SetValue(existingRequest, integrationEvent.LeadId);

        var requests = new List<EnrichmentRequest> { existingRequest };
        var requestSetMock = CreateMockDbSet(requests);

        UnitOfWorkMock
            .Setup(x => x.Set<EnrichmentRequest>())
            .Returns(requestSetMock.Object);

        var wrapper = new IntegrationEventWrapper<LeadCreatedIntegrationEvent>(integrationEvent);

        await _sut.Handle(wrapper, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<EnrichmentRequest>().AddAsync(
            It.IsAny<EnrichmentRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);

        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(
                    $"Lead {integrationEvent.LeadId} already has an enrichment request")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_ShouldCreateRequestWithCorrectData(
        [WithValidLeadCreatedEvent] LeadCreatedIntegrationEvent integrationEvent,
        List<EnrichmentRequest> requests)
    {
        EnrichmentRequest? createdRequest = null; 
        var requestSetMock = CreateMockDbSet(requests);

        UnitOfWorkMock
            .Setup(x => x.Set<EnrichmentRequest>())
            .Returns(requestSetMock.Object);

        UnitOfWorkMock
            .Setup(x => x.Set<EnrichmentRequest>().AddAsync(
                It.IsAny<EnrichmentRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<EnrichmentRequest, CancellationToken>((req, _) => createdRequest = req)
            .ReturnsAsync((EnrichmentRequest _, CancellationToken _) => null!);

        var wrapper = new IntegrationEventWrapper<LeadCreatedIntegrationEvent>(integrationEvent);

        await _sut.Handle(wrapper, CancellationToken.None);

        Assert.That(createdRequest, Is.Not.Null);
        Assert.That(createdRequest!.LeadId, Is.EqualTo(integrationEvent.LeadId));
        Assert.That(createdRequest.CompanyName, Is.EqualTo(integrationEvent.CompanyName));
        Assert.That(createdRequest.Email, Is.EqualTo(integrationEvent.Email));
        Assert.That(createdRequest.ContactPerson, Is.EqualTo(integrationEvent.ContactPerson));
        Assert.That(createdRequest.CustomFields, Is.EqualTo(integrationEvent.CustomFields));
        Assert.That(createdRequest.Status, Is.EqualTo(EnrichmentRequestStatus.Pending));
        Assert.That(createdRequest.RetryCount, Is.EqualTo(0));
    }

    [Test, AutoData]
    public void Handle_WhenInvalidOperationException_ShouldThrow(
        [WithValidLeadCreatedEvent] LeadCreatedIntegrationEvent integrationEvent,
        List<EnrichmentRequest> requests,
        InvalidOperationException exception)
    {
        var requestSetMock = CreateMockDbSet(requests);

        UnitOfWorkMock
            .Setup(x => x.Set<EnrichmentRequest>())
            .Returns(requestSetMock.Object);

        UnitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        var wrapper = new IntegrationEventWrapper<LeadCreatedIntegrationEvent>(integrationEvent);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.Handle(wrapper, CancellationToken.None));
    }
}