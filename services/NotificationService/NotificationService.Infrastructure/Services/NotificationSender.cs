using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Services;
using System.Text.Json;
using NotificationService.Domain.Enums;
using NotificationService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NotificationService.Application.Metrics;
using NotificationService.Domain.Constants;
using NotificationService.Domain.Entities;
using SharedKernel.Json;

namespace NotificationService.Infrastructure.Services;

/// <summary>
/// Реализация отправки уведомлений
/// </summary>
public class NotificationSender(
    ILogger<NotificationSender> logger,
    IEmailSender emailSender,
    ITemplateRenderer templateRenderer,
    ApplicationDbContext dbContext,
    IMemoryCache cache) : INotificationSender
{
    private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss UTC";
    private static readonly TimeSpan TemplateCacheDuration = TimeSpan.FromMinutes(30);

    public async Task<(bool success, string subject, string body)> SendAsync(
        string notificationType,
        NotificationChannel channel,
        string recipient,
        Dictionary<string, string> variables,
        CancellationToken cancellationToken = default)
    {
        if (!variables.ContainsKey(TemplateVariableKeys.Timestamp))
            variables[TemplateVariableKeys.Timestamp] = DateTime.UtcNow.ToString(TimestampFormat);

        var cacheKey = $"notification_template_{notificationType}_{channel}";
        if (!cache.TryGetValue(cacheKey, out NotificationTemplate? template))
        {
            template = await dbContext.NotificationTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TemplateType == notificationType && t.Channel == channel, cancellationToken);

            if (template != null)
                cache.Set(cacheKey, template, TemplateCacheDuration);
        }

        if (template == null)
        {
            logger.LogWarning("No template found for {NotificationType} with channel {Channel}", notificationType, channel);
            NotificationMetrics.NotificationsFailed.Add(1, new TagList 
                { { "type", notificationType }, { "channel", channel.ToString() }, { "reason", "template_not_found" } });
            return (false, string.Empty, string.Empty);
        }

        var subject = templateRenderer.Render(template.SubjectTemplate, variables);
        var body = templateRenderer.Render(template.BodyTemplate, variables);

        if (channel == NotificationChannel.Log)
        {
            var json = JsonSerializer.Serialize(new
            {
                NotificationType = notificationType,
                Channel = channel,
                Recipient = recipient,
                Subject = subject,
                Body = body,
                Timestamp = variables[TemplateVariableKeys.Timestamp]
            }, JsonDefaults.IndentedOptions);

            logger.LogInformation("NOTIFICATION: {Notification}", json);
            NotificationMetrics.NotificationsSent.Add(1, new TagList 
                { { "type", notificationType }, { "channel", "log" } });
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
                    NotificationMetrics.NotificationsSent.Add(1, new TagList 
                        { { "type", notificationType }, { "channel", "email" } });
                }
                else
                {
                    logger.LogWarning("Failed to send email notification to {Recipient}", recipient);
                    NotificationMetrics.NotificationsFailed.Add(1, new TagList 
                        { { "type", notificationType }, { "channel", "email" }, { "reason", "send_failed" } });
                }
                return (success, subject, body);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending email notification to {Recipient}", recipient);
                var reason = ex.Message.Contains("timeout") ? "timeout" : "exception";
                NotificationMetrics.NotificationsFailed.Add(1, new TagList 
                    { { "type", notificationType }, { "channel", "email" }, { "reason", reason } });
                return (false, subject, body);
            }
        }

        logger.LogInformation("Notification [{Channel}] sent to {Recipient}: {Notification}", channel, recipient, 
            JsonSerializer.Serialize(new { NotificationType = notificationType, Channel = channel, Recipient = recipient, Subject = subject, Body = body }, JsonDefaults.IndentedOptions));
        NotificationMetrics.NotificationsSent.Add(1, new TagList 
            { { "type", notificationType }, { "channel", channel.ToString() } });
        return (true, subject, body);
    }
}