using AvroSchemas.Messages.LeadEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Application.Services;
using SharedInfrastructure.Telemetry;
using SharedKernel.Base;

namespace NotificationService.Application.EventHandlers;

/// <summary>
/// Обработчик события LeadCreated
/// </summary>
public class LeadCreatedEventHandler(
    IUnitOfWork unitOfWork,
    INotificationSender notificationSender,
    ILogger<LeadCreatedEventHandler> logger)
    : INotificationHandler<LeadCreated>
{
    public async Task Handle(LeadCreated @event, CancellationToken cancellationToken)
    {
        using var activity = TelemetryConstants.ActivitySource.StartEventHandlerSpan("LeadCreated")!
            .AddTags(
                (TelemetryAttributes.LeadId, @event.LeadId),
                (TelemetryAttributes.EventName, "LeadCreated"),
                (TelemetryAttributes.LeadCompany, @event.CompanyName),
                (TelemetryAttributes.LeadEmail, @event.Email),
                (TelemetryAttributes.LeadSource, @event.Source),
                (TelemetryAttributes.ProcessingStep, "notification_created"));

        var variables = new Dictionary<string, string>
        {
            ["LeadId"] = @event.LeadId.ToString(),
            ["CompanyName"] = @event.CompanyName,
            ["ContactPerson"] = @event.ContactPerson ?? "Unknown",
            ["Email"] = @event.Email,
            ["Source"] = @event.Source
        };

        var (success, subject, body) = await notificationSender.SendAsync(
            "LeadCreated",
            NotificationChannel.Email,
            @event.Email,
            variables,
            cancellationToken);

        if (success)
        {
            var notification = Notification.Create(
                @event.LeadId,
                @event.EventId.ToString(),
                "LeadCreated",
                NotificationChannel.Email,
                @event.Email,
                body,
                subject);

            await unitOfWork.Set<Notification>().AddAsync(notification, cancellationToken);
            notification.MarkAsSent();
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("LeadCreated notification sent for lead {LeadId}", @event.LeadId);
        }
    }
}