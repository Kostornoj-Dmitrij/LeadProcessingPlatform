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
/// Обработчик события LeadDistributed
/// </summary>
public class LeadDistributedEventHandler(
    IUnitOfWork unitOfWork,
    INotificationSender notificationSender,
    ILogger<LeadDistributedEventHandler> logger)
    : INotificationHandler<LeadDistributed>
{
    public async Task Handle(LeadDistributed @event, CancellationToken cancellationToken)
    {
        using var activity = ActivityBuilderExtensions.CreateEventActivity(@event)
            .WithTag(TelemetryAttributes.DistributionTarget, @event.Target)
            .WithProcessingStep("notification_distributed");

        logger.LogInformation("Processing LeadDistributed notification for lead {LeadId}", @event.LeadId);

        var variables = new Dictionary<string, string>
        {
            [TemplateVariableKeys.LeadId] = @event.LeadId.ToString(),
            [TemplateVariableKeys.Target] = @event.Target
        };

        var (success, subject, body) = await notificationSender.SendAsync(
            NotificationTypeConstants.LeadDistributed,
            NotificationChannel.Email,
            RecipientConstants.Sales,
            variables,
            cancellationToken);

        if (success)
        {
            var notification = Notification.Create(
                @event.LeadId,
                @event.EventId.ToString(),
                NotificationTypeConstants.LeadDistributed,
                NotificationChannel.Email,
                RecipientConstants.Sales,
                body,
                subject);

            await unitOfWork.Set<Notification>().AddAsync(notification, cancellationToken);
            notification.MarkAsSent();
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("LeadDistributed notification sent for lead {LeadId}", @event.LeadId);
        }
    }
}