using AutoFixture.NUnit4;
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
/// Тесты для LeadRejectedEventHandler
/// </summary>
[Category("Application")]
public class LeadRejectedEventHandlerTests : DatabaseTestBase
{
    private const string NotificationType = "LeadRejected";
    private const string SupportEmail = "support@example.com";

    private Mock<INotificationSender> _notificationSenderMock = null!;
    private Mock<ILogger<LeadRejectedEventHandler>> _loggerMock = null!;
    private LeadRejectedEventHandler _sut = null!;

    [SetUp]
    public void Setup()
    {
        BaseSetup();
        _notificationSenderMock = new Mock<INotificationSender>();
        _loggerMock = new Mock<ILogger<LeadRejectedEventHandler>>();
        _sut = new LeadRejectedEventHandler(
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
        [WithValidLeadRejectedEvent] LeadRejected @event,
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
                SupportEmail,
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
    public async Task Handle_ShouldPassAllFailureDetailsInVariables(
        [WithValidLeadRejectedEvent] LeadRejected @event,
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
        Assert.That(capturedVariables!["Reason"], Is.EqualTo(@event.Reason));
        Assert.That(capturedVariables!["FailureType"], Is.EqualTo(@event.FailureType));
        Assert.That(capturedVariables!["ErrorDetails"], Is.EqualTo(@event.ErrorDetails ?? "N/A"));
    }
}