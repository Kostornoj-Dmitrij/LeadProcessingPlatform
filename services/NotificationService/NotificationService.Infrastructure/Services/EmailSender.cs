using Microsoft.Extensions.Logging;
using NotificationService.Application.Services;

namespace NotificationService.Infrastructure.Services;

/// <summary>
/// Эмулятор отправки email
/// </summary>
public class EmailSender(ILogger<EmailSender> logger) : IEmailSender
{
    public Task<bool> SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("EMAIL SEND (emulated): To={To}, Subject={Subject}, Body={Body}", 
            to, subject, body.Length > 200 ? body[..200] + "..." : body);

        return Task.FromResult(true);
    }
}