using AutoFixture.NUnit3;
using IntegrationEvents.DistributionEvents;
using LeadService.Application.EventHandlers;
using LeadService.Domain.Entities;
using LeadService.Domain.Enums;
using LeadService.Tests.Common.Attributes;
using LeadService.Tests.Common.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SharedKernel.Events;

namespace LeadService.Tests.Application.EventHandlers;

/// <summary>
/// Тесты для DistributionSucceededEventHandler
/// </summary>
[Category("Application")]
public class DistributionSucceededEventHandlerTests : DatabaseTestBase
{
    private static readonly Type LeadType = typeof(Lead);

    private Mock<ILogger<DistributionSucceededEventHandler>> _loggerMock = null!;
    private DistributionSucceededEventHandler _sut = null!;

    [SetUp]
    public void Setup()
    {
        BaseSetup();
        _loggerMock = new Mock<ILogger<DistributionSucceededEventHandler>>();
        _sut = new DistributionSucceededEventHandler(UnitOfWorkMock.Object, _loggerMock.Object);
    }

    [TearDown]
    public void Cleanup()
    {
        BaseCleanup();
        _loggerMock.Reset();
    }

    [Test, AutoData]
    public async Task Handle_WhenLeadExists_ShouldMarkDistributedAndClose(
        [WithValidLead(LeadStatus.Qualified)] Lead lead,
        [WithDistributionSucceededEvent] DistributionSucceededIntegrationEvent integrationEvent)
    {
        LeadType.GetProperty(nameof(Lead.Id))?.SetValue(lead, integrationEvent.LeadId);

        var leads = new List<Lead> { lead };
        var leadSetMock = CreateMockDbSet(leads);

        UnitOfWorkMock
            .Setup(x => x.Set<Lead>())
            .Returns(leadSetMock.Object);

        var wrapper = new IntegrationEventWrapper<DistributionSucceededIntegrationEvent>(integrationEvent);

        await _sut.Handle(wrapper, CancellationToken.None);

        Assert.That(lead.Status, Is.EqualTo(LeadStatus.Closed));
        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Test, AutoData]
    public async Task Handle_WhenLeadNotFound_ShouldLogWarning(
        [WithDistributionSucceededEvent] DistributionSucceededIntegrationEvent integrationEvent)
    {
        var leads = new List<Lead>();
        var leadSetMock = CreateMockDbSet(leads);

        UnitOfWorkMock
            .Setup(x => x.Set<Lead>())
            .Returns(leadSetMock.Object);

        var wrapper = new IntegrationEventWrapper<DistributionSucceededIntegrationEvent>(integrationEvent);

        await _sut.Handle(wrapper, CancellationToken.None);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains($"Lead not found: {integrationEvent.LeadId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test, AutoData]
    public void Handle_WhenConcurrencyException_ShouldThrow(
        [WithValidLead(LeadStatus.Qualified)] Lead lead,
        [WithDistributionSucceededEvent] DistributionSucceededIntegrationEvent integrationEvent)
    {
        LeadType.GetProperty(nameof(Lead.Id))?.SetValue(lead, integrationEvent.LeadId);

        var leads = new List<Lead> { lead };
        var leadSetMock = CreateMockDbSet(leads);

        UnitOfWorkMock
            .Setup(x => x.Set<Lead>())
            .Returns(leadSetMock.Object);

        UnitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        var wrapper = new IntegrationEventWrapper<DistributionSucceededIntegrationEvent>(integrationEvent);

        Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => 
            _sut.Handle(wrapper, CancellationToken.None));
    }

    [Test, AutoData]
    public void Handle_WhenGenericException_ShouldLogErrorAndThrow(
        [WithValidLead(LeadStatus.Qualified)] Lead lead,
        [WithDistributionSucceededEvent] DistributionSucceededIntegrationEvent integrationEvent,
        InvalidOperationException exception)
    {
        LeadType.GetProperty(nameof(Lead.Id))?.SetValue(lead, integrationEvent.LeadId);

        var leads = new List<Lead> { lead };
        var leadSetMock = CreateMockDbSet(leads);

        UnitOfWorkMock
            .Setup(x => x.Set<Lead>())
            .Returns(leadSetMock.Object);

        UnitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        var wrapper = new IntegrationEventWrapper<DistributionSucceededIntegrationEvent>(integrationEvent);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => 
            _sut.Handle(wrapper, CancellationToken.None));

        Assert.That(ex.Message, Is.EqualTo(exception.Message));

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains($"Error processing distribution success for lead {integrationEvent.LeadId}")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}