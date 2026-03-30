using Microsoft.Extensions.Logging;
using NotificationService.Application.Services;
using System.Text.Json;
using NotificationService.Domain.Enums;
using NotificationService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace NotificationService.Infrastructure.Services;

/// <summary>
/// Реализация отправки уведомлений
/// </summary>
public class NotificationSender(
    ILogger<NotificationSender> logger,
    IEmailSender emailSender,
    ITemplateRenderer templateRenderer,
    ApplicationDbContext dbContext) : INotificationSender
{
    public async Task<(bool success, string subject, string body)> SendAsync(
        string notificationType,
        NotificationChannel channel,
        string recipient,
        Dictionary<string, string> variables,
        CancellationToken cancellationToken = default)
    {
        if (!variables.ContainsKey("Timestamp"))
            variables["Timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");

        var template = await dbContext.NotificationTemplates
            .FirstOrDefaultAsync(t => t.TemplateType == notificationType && t.Channel == channel, cancellationToken);

        if (template == null)
        {
            logger.LogWarning("No template found for {NotificationType} with channel {Channel}", notificationType, channel);
            return (false, string.Empty, string.Empty);
        }

        var subject = templateRenderer.Render(template.SubjectTemplate, variables);
        var body = templateRenderer.Render(template.BodyTemplate, variables);

        var notification = new
        {
            NotificationType = notificationType,
            Channel = channel,
            Recipient = recipient,
            Subject = subject,
            Body = body,
            Timestamp = variables["Timestamp"]
        };

        var json = JsonSerializer.Serialize(notification, new JsonSerializerOptions { WriteIndented = true });

        if (channel == NotificationChannel.Log)
        {
            logger.LogInformation("NOTIFICATION: {Notification}", json);
            return (true, subject, body);
        }

        if (channel == NotificationChannel.Email)
        {
            try
            {
                var success = await emailSender.SendEmailAsync(recipient, subject, body, cancellationToken);
                if (success)
                {
                    logger.LogInformation("Email notification sent: {Subject} to {Recipient}", subject, recipient);
                }
                else
                {
                    logger.LogWarning("Failed to send email notification to {Recipient}", recipient);
                }
                return (success, subject, body);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending email notification to {Recipient}", recipient);
                return (false, subject, body);
            }
        }

        logger.LogInformation("Notification [{Channel}] sent to {Recipient}: {Notification}", channel, recipient, json);
        return (true, subject, body);
    }
}