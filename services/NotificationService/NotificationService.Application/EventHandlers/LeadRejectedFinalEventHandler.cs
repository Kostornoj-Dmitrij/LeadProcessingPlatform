using AvroSchemas.Messages.LeadEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Application.Services;
using NotificationService.Domain.Constants;
using SharedInfrastructure.Telemetry;
using SharedKernel.Base;

namespace NotificationService.Application.EventHandlers;

/// <summary>
/// Обработчик события LeadRejectedFinal
/// </summary>
public class LeadRejectedFinalEventHandler(
    IUnitOfWork unitOfWork,
    INotificationSender notificationSender,
    ILogger<LeadRejectedFinalEventHandler> logger)
    : INotificationHandler<LeadRejectedFinal>
{
    public async Task Handle(LeadRejectedFinal @event, CancellationToken cancellationToken)
    {
        using var activity = ActivityBuilderExtensions.CreateEventActivity(@event)
            .WithTag(TelemetryAttributes.LeadStatus, @event.FinalStatus)
            .WithProcessingStep("notification_rejected_final");

        logger.LogInformation("Processing LeadRejectedFinal notification for lead {LeadId}", @event.LeadId);

        var variables = new Dictionary<string, string>
        {
            [TemplateVariableKeys.LeadId] = @event.LeadId.ToString(),
            [TemplateVariableKeys.FinalStatus] = @event.FinalStatus
        };

        var (success, subject, body) = await notificationSender.SendAsync(
            NotificationTypeConstants.LeadRejectedFinal,
            NotificationChannel.Email,
            RecipientConstants.Analytics,
            variables,
            cancellationToken);

        if (success)
        {
            var notification = Notification.Create(
                @event.LeadId,
                @event.EventId.ToString(),
                NotificationTypeConstants.LeadRejectedFinal,
                NotificationChannel.Email,
                RecipientConstants.Analytics,
                body,
                subject);

            await unitOfWork.Set<Notification>().AddAsync(notification, cancellationToken);
            notification.MarkAsSent();
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("LeadRejectedFinal notification sent for lead {LeadId}", @event.LeadId);
        }
    }
}