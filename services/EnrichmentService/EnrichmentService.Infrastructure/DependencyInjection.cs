using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EnrichmentService.Application.Common.Interfaces;
using EnrichmentService.Infrastructure.Background;
using EnrichmentService.Infrastructure.Data;
using EnrichmentService.Infrastructure.EventBus;
using EnrichmentService.Infrastructure.Inbox;
using EnrichmentService.Infrastructure.Outbox;
using EnrichmentService.Infrastructure.Clients;
using SharedKernel.Base;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EnrichmentService.Infrastructure;

/// <summary>
/// Регистрация зависимостей слоя Infrastructure
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
            options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
                .UseLoggerFactory(sp.GetRequiredService<ILoggerFactory>()));

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<IDomainEventToOutboxConverter, DomainEventToOutboxConverter>();

        services.AddSingleton<KafkaEventBus>();
        services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<KafkaEventBus>());

        services.AddScoped<IInboxStore, InboxStore>();
        services.AddSingleton<IDeadLetterQueue, KafkaDeadLetterQueue>();
        services.AddHostedService<InboxProcessor>();

        services.AddHostedService<KafkaConsumer>();
        services.AddScoped<IKafkaConsumer>(sp =>
            sp.GetServices<IHostedService>().OfType<KafkaConsumer>().FirstOrDefault() 
            ?? throw new InvalidOperationException("KafkaConsumer not found"));

        services.AddHostedService<OutboxPublisher>();
        services.AddHostedService<EnrichmentProcessor>();

        services.AddHttpClient<IExternalEnrichmentClient, ExternalEnrichmentClient>();

        return services;
    }
}