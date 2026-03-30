using MediatR;
using Microsoft.Extensions.Logging;
using AvroSchemas.Messages.LeadEvents;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Application.Services;
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
        logger.LogInformation("Processing LeadCreated notification for lead {LeadId}", @event.LeadId);

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