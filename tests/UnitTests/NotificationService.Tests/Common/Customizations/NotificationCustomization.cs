using AutoFixture;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;

namespace NotificationService.Tests.Common.Customizations;

/// <summary>
/// Кастомизация для Notification
/// </summary>
public class NotificationCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<Notification>(composer => composer
            .FromFactory(() =>
            {
                var leadId = fixture.Create<Guid>();
                var eventId = fixture.Create<Guid>().ToString();
                var notificationType = "LeadCreated";
                var channel = NotificationChannel.Email;
                var recipient = $"{fixture.Create<string>().ToLower()}@example.com";
                var body = fixture.Create<string>();
                var subject = fixture.Create<string>();

                return Notification.Create(
                    leadId,
                    eventId,
                    notificationType,
                    channel,
                    recipient,
                    body,
                    subject);
            }));
    }
}