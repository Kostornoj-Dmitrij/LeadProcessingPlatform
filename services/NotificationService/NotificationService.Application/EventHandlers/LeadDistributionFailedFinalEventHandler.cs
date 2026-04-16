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
/// Обработчик события LeadDistributionFailedFinal
/// </summary>
public class LeadDistributionFailedFinalEventHandler(
    IUnitOfWork unitOfWork,
    INotificationSender notificationSender,
    ILogger<LeadDistributionFailedFinalEventHandler> logger)
    : INotificationHandler<LeadDistributionFailedFinal>
{
    public async Task Handle(LeadDistributionFailedFinal @event, CancellationToken cancellationToken)
    {
        using var activity = ActivityBuilderExtensions.CreateEventActivity(@event)
            .WithTag(TelemetryAttributes.LeadStatus, @event.FinalStatus)
            .WithProcessingStep("notification_distribution_failed_final");

        logger.LogInformation("Processing LeadDistributionFailedFinal notification for lead {LeadId}", @event.LeadId);

        var variables = new Dictionary<string, string>
        {
            ["LeadId"] = @event.LeadId.ToString(),
            ["FinalStatus"] = @event.FinalStatus
        };

        var (success, subject, body) = await notificationSender.SendAsync(
            "LeadDistributionFailedFinal",
            NotificationChannel.Email,
            "analytics@example.com",
            variables,
            cancellationToken);

        if (success)
        {
            var notification = Notification.Create(
                @event.LeadId,
                @event.EventId.ToString(),
                "LeadDistributionFailedFinal",
                NotificationChannel.Email,
                "analytics@example.com",
                body,
                subject);

            await unitOfWork.Set<Notification>().AddAsync(notification, cancellationToken);
            notification.MarkAsSent();
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("LeadDistributionFailedFinal notification sent for lead {LeadId}", @event.LeadId);
        }
    }
}