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
        using var activity = ActivityBuilderExtensions.CreateEventActivity(@event)
            .WithFailureTags(reason: @event.Reason)
            .WithProcessingStep("notification_distribution_failed");

        logger.LogInformation("Processing LeadDistributionFailed notification for lead {LeadId}", @event.LeadId);

        var variables = new Dictionary<string, string>
        {
            [TemplateVariableKeys.LeadId] = @event.LeadId.ToString(),
            [TemplateVariableKeys.Reason] = @event.Reason
        };

        var (success, subject, body) = await notificationSender.SendAsync(
            NotificationTypeConstants.LeadDistributionFailed,
            NotificationChannel.Email,
            RecipientConstants.Support,
            variables,
            cancellationToken);

        if (success)
        {
            var notification = Notification.Create(
                @event.LeadId,
                @event.EventId.ToString(),
                NotificationTypeConstants.LeadDistributionFailed,
                NotificationChannel.Email,
                RecipientConstants.Support,
                body,
                subject);

            await unitOfWork.Set<Notification>().AddAsync(notification, cancellationToken);
            notification.MarkAsSent();
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("LeadDistributionFailed notification sent for lead {LeadId}", @event.LeadId);
        }
    }
}