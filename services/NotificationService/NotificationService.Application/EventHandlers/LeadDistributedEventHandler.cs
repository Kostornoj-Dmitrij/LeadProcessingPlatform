using MediatR;
using Microsoft.Extensions.Logging;
using AvroSchemas.Messages.LeadEvents;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Application.Services;
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
        logger.LogInformation("Processing LeadDistributed notification for lead {LeadId}", @event.LeadId);

        var variables = new Dictionary<string, string>
        {
            ["LeadId"] = @event.LeadId.ToString(),
            ["Target"] = @event.Target
        };

        var (success, subject, body) = await notificationSender.SendAsync(
            "LeadDistributed",
            NotificationChannel.Email,
            "sales@example.com",
            variables,
            cancellationToken);

        if (success)
        {
            var notification = Notification.Create(
                @event.LeadId,
                @event.EventId.ToString(),
                "LeadDistributed",
                NotificationChannel.Email,
                "sales@example.com",
                body,
                subject);

            await unitOfWork.Set<Notification>().AddAsync(notification, cancellationToken);
            notification.MarkAsSent();
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("LeadDistributed notification sent for lead {LeadId}", @event.LeadId);
        }
    }
}