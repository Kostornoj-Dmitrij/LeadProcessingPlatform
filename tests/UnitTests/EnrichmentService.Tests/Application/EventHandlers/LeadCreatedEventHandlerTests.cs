using AutoFixture.NUnit4;
using AvroSchemas.Messages.LeadEvents;
using EnrichmentService.Application.EventHandlers;
using EnrichmentService.Domain.Entities;
using EnrichmentService.Domain.Enums;
using EnrichmentService.Tests.Common.Attributes;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
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
        [WithValidLeadCreatedEvent] LeadCreated @event,
        List<EnrichmentRequest> requests)
    {
        var requestSetMock = CreateMockDbSet(requests);

        UnitOfWorkMock
            .Setup(x => x.Set<EnrichmentRequest>())
            .Returns(requestSetMock.Object);

        await _sut.Handle(@event, CancellationToken.None);

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
                    $"Enrichment request created for lead {@event.LeadId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_WhenExistingRequestExists_ShouldSkip(
        [WithValidLeadCreatedEvent] LeadCreated @event,
        [WithValidEnrichmentRequest] EnrichmentRequest existingRequest)
    {
        RequestType.GetProperty(nameof(EnrichmentRequest.LeadId))?.SetValue(existingRequest, @event.LeadId);

        var requests = new List<EnrichmentRequest> { existingRequest };
        var requestSetMock = CreateMockDbSet(requests);

        UnitOfWorkMock
            .Setup(x => x.Set<EnrichmentRequest>())
            .Returns(requestSetMock.Object);

        await _sut.Handle(@event, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<EnrichmentRequest>().AddAsync(
            It.IsAny<EnrichmentRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);

        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(
                    $"Lead {@event.LeadId} already has an enrichment request")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_ShouldCreateRequestWithCorrectData(
        [WithValidLeadCreatedEvent] LeadCreated @event,
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

        await _sut.Handle(@event, CancellationToken.None);

        Assert.That(createdRequest, Is.Not.Null);
        Assert.That(createdRequest!.LeadId, Is.EqualTo(@event.LeadId));
        Assert.That(createdRequest.CompanyName, Is.EqualTo(@event.CompanyName));
        Assert.That(createdRequest.Email, Is.EqualTo(@event.Email));
        Assert.That(createdRequest.ContactPerson, Is.EqualTo(@event.ContactPerson));
        Assert.That(createdRequest.CustomFields, Is.EqualTo(@event.CustomFields));
        Assert.That(createdRequest.Status, Is.EqualTo(EnrichmentRequestStatus.Pending));
        Assert.That(createdRequest.RetryCount, Is.EqualTo(0));
    }

    [Test, AutoData]
    public void Handle_WhenInvalidOperationException_ShouldThrow(
        [WithValidLeadCreatedEvent] LeadCreated @event,
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

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.Handle(@event, CancellationToken.None));
    }
}