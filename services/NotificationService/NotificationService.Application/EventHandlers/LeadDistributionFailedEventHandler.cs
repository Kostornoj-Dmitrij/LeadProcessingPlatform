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
/// Обработчик события LeadDistributionFailed
/// </summary>
public class LeadDistributionFailedEventHandler(
    IUnitOfWork unitOfWork,
    INotificationSender notificationSender,
    ILogger<LeadDistributionFailedEventHandler> logger)
    : INotificationHandler<LeadDistributionFailed>
{
    public async Task Handle(LeadDistributionFailed @event, CancellationToken cancellationToken)
    {
        using var activity = TelemetryConstants.ActivitySource.StartEventHandlerSpan("LeadDistributionFailed")!
            .AddTags(
                (TelemetryAttributes.LeadId, @event.LeadId),
                (TelemetryAttributes.EventName, "LeadDistributionFailed"),
                (TelemetryAttributes.FailureReason, @event.Reason),
                (TelemetryAttributes.ProcessingStep, "notification_distribution_failed"));
        logger.LogInformation("Processing LeadDistributionFailed notification for lead {LeadId}", @event.LeadId);

        var variables = new Dictionary<string, string>
        {
            ["LeadId"] = @event.LeadId.ToString(),
            ["Reason"] = @event.Reason
        };

        var (success, subject, body) = await notificationSender.SendAsync(
            "LeadDistributionFailed",
            NotificationChannel.Email,
            "support@example.com",
            variables,
            cancellationToken);

        if (success)
        {
            var notification = Notification.Create(
                @event.LeadId,
                @event.EventId.ToString(),
                "LeadDistributionFailed",
                NotificationChannel.Email,
                "support@example.com",
                body,
                subject);

            await unitOfWork.Set<Notification>().AddAsync(notification, cancellationToken);
            notification.MarkAsSent();
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("LeadDistributionFailed notification sent for lead {LeadId}", @event.LeadId);
        }
    }
}