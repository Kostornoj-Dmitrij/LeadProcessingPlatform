using AutoFixture.NUnit3;
using AvroSchemas.Messages.LeadEvents;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using NotificationService.Application.EventHandlers;
using NotificationService.Application.Services;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Tests.Common.Attributes;
using SharedTestInfrastructure.Database;

namespace NotificationService.Tests.Application.EventHandlers;

/// <summary>
/// Тесты для LeadDistributedEventHandler
/// </summary>
[Category("Application")]
public class LeadDistributedEventHandlerTests : DatabaseTestBase
{
    private const string NotificationType = "LeadDistributed";
    private const string SalesEmail = "sales@example.com";

    private Mock<INotificationSender> _notificationSenderMock = null!;
    private Mock<ILogger<LeadDistributedEventHandler>> _loggerMock = null!;
    private LeadDistributedEventHandler _sut = null!;

    [SetUp]
    public void Setup()
    {
        BaseSetup();
        _notificationSenderMock = new Mock<INotificationSender>();
        _loggerMock = new Mock<ILogger<LeadDistributedEventHandler>>();
        _sut = new LeadDistributedEventHandler(
            UnitOfWorkMock.Object,
            _notificationSenderMock.Object,
            _loggerMock.Object);
    }

    [TearDown]
    public void Cleanup()
    {
        BaseCleanup();
        _notificationSenderMock.Reset();
        _loggerMock.Reset();
    }

    [Test, AutoData]
    public async Task Handle_WhenNotificationSucceeds_ShouldSaveNotificationAndMarkAsSent(
        [WithValidLeadDistributedEvent] LeadDistributed @event,
        string subject,
        string body)
    {
        var notifications = new List<Notification>();
        var notificationSetMock = CreateMockDbSet(notifications);

        UnitOfWorkMock.Setup(x => x.Set<Notification>()).Returns(notificationSetMock.Object);
        _notificationSenderMock
            .Setup(x => x.SendAsync(
                NotificationType,
                NotificationChannel.Email,
                SalesEmail,
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, subject, body));

        await _sut.Handle(@event, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<Notification>().AddAsync(
            It.IsAny<Notification>(),
            It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains(NotificationType + " notification sent")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_ShouldSendToSalesEmail(
        [WithValidLeadDistributedEvent] LeadDistributed @event,
        List<Notification> notifications,
        string subject,
        string body)
    {
        var notificationSetMock = CreateMockDbSet(notifications);
        string? capturedRecipient = null;

        UnitOfWorkMock.Setup(x => x.Set<Notification>()).Returns(notificationSetMock.Object);
        _notificationSenderMock
            .Setup(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<NotificationChannel>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, NotificationChannel, string, Dictionary<string, string>, CancellationToken>(
                (_, _, recipient, _, _) => capturedRecipient = recipient)
            .ReturnsAsync((true, subject, body));

        await _sut.Handle(@event, CancellationToken.None);

        Assert.That(capturedRecipient, Is.EqualTo(SalesEmail));
    }

    [Test, AutoData]
    public async Task Handle_ShouldPassTargetInVariables(
        [WithValidLeadDistributedEvent] LeadDistributed @event,
        List<Notification> notifications,
        string subject,
        string body)
    {
        var notificationSetMock = CreateMockDbSet(notifications);
        Dictionary<string, string>? capturedVariables = null;

        UnitOfWorkMock.Setup(x => x.Set<Notification>()).Returns(notificationSetMock.Object);
        _notificationSenderMock
            .Setup(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<NotificationChannel>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, NotificationChannel, string, Dictionary<string, string>, CancellationToken>(
                (_, _, _, vars, _) => capturedVariables = vars)
            .ReturnsAsync((true, subject, body));

        await _sut.Handle(@event, CancellationToken.None);

        Assert.That(capturedVariables, Is.Not.Null);
        Assert.That(capturedVariables!["LeadId"], Is.EqualTo(@event.LeadId.ToString()));
        Assert.That(capturedVariables!["Target"], Is.EqualTo(@event.Target));
    }
}