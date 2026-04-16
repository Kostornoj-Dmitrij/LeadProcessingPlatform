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
/// Обработчик события LeadRejected
/// </summary>
public class LeadRejectedEventHandler(
    IUnitOfWork unitOfWork,
    INotificationSender notificationSender,
    ILogger<LeadRejectedEventHandler> logger)
    : INotificationHandler<LeadRejected>
{
    public async Task Handle(LeadRejected @event, CancellationToken cancellationToken)
    {
        using var activity = ActivityBuilderExtensions.CreateEventActivity(@event)
            .WithFailureTags(reason: @event.Reason, failureType: @event.FailureType)
            .WithProcessingStep("notification_rejected");

        logger.LogInformation("Processing LeadRejected notification for lead {LeadId}", @event.LeadId);

        var variables = new Dictionary<string, string>
        {
            [TemplateVariableKeys.LeadId] = @event.LeadId.ToString(),
            [TemplateVariableKeys.Reason] = @event.Reason,
            [TemplateVariableKeys.FailureType] = @event.FailureType,
            [TemplateVariableKeys.ErrorDetails] = @event.ErrorDetails ?? "N/A"
        };

        var (success, subject, body) = await notificationSender.SendAsync(
            NotificationTypeConstants.LeadRejected,
            NotificationChannel.Email,
            RecipientConstants.Support,
            variables,
            cancellationToken);

        if (success)
        {
            var notification = Notification.Create(
                @event.LeadId,
                @event.EventId.ToString(),
                NotificationTypeConstants.LeadRejected,
                NotificationChannel.Email,
                RecipientConstants.Support,
                body,
                subject);

            await unitOfWork.Set<Notification>().AddAsync(notification, cancellationToken);
            notification.MarkAsSent();
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("LeadRejected notification sent for lead {LeadId}", @event.LeadId);
        }
    }
}