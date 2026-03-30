using MediatR;
using Microsoft.Extensions.Logging;
using AvroSchemas.Messages.LeadEvents;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Application.Services;
using SharedKernel.Base;

namespace NotificationService.Application.EventHandlers;

/// <summary>
/// Обработчик события LeadQualified
/// </summary>
public class LeadQualifiedEventHandler(
    IUnitOfWork unitOfWork,
    INotificationSender notificationSender,
    ILogger<LeadQualifiedEventHandler> logger)
    : INotificationHandler<LeadQualified>
{
    public async Task Handle(LeadQualified @event, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing LeadQualified notification for lead {LeadId}", @event.LeadId);

        var variables = new Dictionary<string, string>
        {
            ["LeadId"] = @event.LeadId.ToString(),
            ["CompanyName"] = @event.CompanyName,
            ["ContactPerson"] = @event.ContactPerson ?? "Unknown",
            ["Email"] = @event.Email,
            ["Score"] = @event.Score.ToString(),
            ["Industry"] = @event.EnrichedData?.Industry ?? "Unknown",
            ["CompanySize"] = @event.EnrichedData?.CompanySize ?? "Unknown"
        };

        var (success, subject, body) = await notificationSender.SendAsync(
            "LeadQualified",
            NotificationChannel.Email,
            @event.Email,
            variables,
            cancellationToken);

        if (success)
        {
            var notification = Notification.Create(
                @event.LeadId,
                @event.EventId.ToString(),
                "LeadQualified",
                NotificationChannel.Email,
                @event.Email,
                body,
                subject);

            await unitOfWork.Set<Notification>().AddAsync(notification, cancellationToken);
            notification.MarkAsSent();
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("LeadQualified notification sent for lead {LeadId}", @event.LeadId);
        }
    }
}