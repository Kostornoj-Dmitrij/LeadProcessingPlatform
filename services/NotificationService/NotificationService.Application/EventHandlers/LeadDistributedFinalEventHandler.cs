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
/// Обработчик события LeadDistributedFinal
/// </summary>
public class LeadDistributedFinalEventHandler(
    IUnitOfWork unitOfWork,
    INotificationSender notificationSender,
    ILogger<LeadDistributedFinalEventHandler> logger)
    : INotificationHandler<LeadDistributedFinal>
{
    public async Task Handle(LeadDistributedFinal @event, CancellationToken cancellationToken)
    {
        using var activity = ActivityBuilderExtensions.CreateEventActivity(@event)
            .WithTag(TelemetryAttributes.LeadStatus, @event.FinalStatus)
            .WithProcessingStep("notification_final");

        logger.LogInformation("Processing LeadDistributedFinal notification for lead {LeadId}", @event.LeadId);

        var variables = new Dictionary<string, string>
        {
            ["LeadId"] = @event.LeadId.ToString(),
            ["FinalStatus"] = @event.FinalStatus
        };

        var (success, subject, body) = await notificationSender.SendAsync(
            "LeadDistributedFinal",
            NotificationChannel.Email,
            "analytics@example.com",
            variables,
            cancellationToken);

        if (success)
        {
            var notification = Notification.Create(
                @event.LeadId,
                @event.EventId.ToString(),
                "LeadDistributedFinal",
                NotificationChannel.Email,
                "analytics@example.com",
                body,
                subject);

            await unitOfWork.Set<Notification>().AddAsync(notification, cancellationToken);
            notification.MarkAsSent();
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("LeadDistributedFinal notification sent for lead {LeadId}", @event.LeadId);
        }
    }
}