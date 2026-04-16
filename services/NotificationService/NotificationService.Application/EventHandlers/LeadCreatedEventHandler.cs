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
        using var activity = ActivityBuilderExtensions.CreateEventActivity(@event)
            .WithTags(
                (TelemetryAttributes.LeadCompany, @event.CompanyName),
                (TelemetryAttributes.LeadEmail, @event.Email),
                (TelemetryAttributes.LeadSource, @event.Source))
            .WithProcessingStep("notification_created");

        var variables = new Dictionary<string, string>
        {
            [TemplateVariableKeys.LeadId] = @event.LeadId.ToString(),
            [TemplateVariableKeys.CompanyName] = @event.CompanyName,
            [TemplateVariableKeys.ContactPerson] = @event.ContactPerson ?? "Unknown",
            [TemplateVariableKeys.Email] = @event.Email,
            [TemplateVariableKeys.Source] = @event.Source
        };

        var (success, subject, body) = await notificationSender.SendAsync(
            NotificationTypeConstants.LeadCreated,
            NotificationChannel.Email,
            @event.Email,
            variables,
            cancellationToken);

        if (success)
        {
            var notification = Notification.Create(
                @event.LeadId,
                @event.EventId.ToString(),
                NotificationTypeConstants.LeadCreated,
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