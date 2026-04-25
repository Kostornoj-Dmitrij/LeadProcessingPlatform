using AvroSchemas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Application.Services;
using NotificationService.Infrastructure.Data;
using NotificationService.Infrastructure.Outbox;
using NotificationService.Infrastructure.Services;
using SharedInfrastructure;
using SharedInfrastructure.Outbox;

namespace NotificationService.Infrastructure;

/// <summary>
/// Регистрация зависимостей слоя Infrastructure
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();

        services.AddScoped<IDomainEventToOutboxConverter, DomainEventToOutboxConverter>();

        services.AddScoped<INotificationSender, NotificationSender>();
        services.AddScoped<IEmailSender, EmailSender>();
        services.AddScoped<ITemplateRenderer, TemplateRenderer>();

        var topics = new[]
        {
            KafkaTopics.LeadEventsBase,
            KafkaTopics.DistributionEventsBase
        };

        services.AddSharedInfrastructure<ApplicationDbContext>(
            configuration,
            "notification-service",
            topics);

        return services;
    }
}