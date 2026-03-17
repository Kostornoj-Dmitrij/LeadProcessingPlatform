using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LeadService.Application.Common.Interfaces;
using LeadService.Infrastructure.Data;
using LeadService.Infrastructure.Data.Repositories;
using LeadService.Infrastructure.EventBus;
using LeadService.Infrastructure.Outbox;
using LeadService.Infrastructure.Inbox;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharedKernel.Base;

namespace LeadService.Infrastructure;

/// <summary>
/// Регистрация зависимостей слоя Infrastructure
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
            .UseLoggerFactory(sp.GetRequiredService<ILoggerFactory>()));
        
        services.AddScoped<IUnitOfWork>(provider => 
            provider.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();
        services.AddScoped<IDomainEventToOutboxConverter, DomainEventToOutboxConverter>();

        services.AddSingleton<KafkaEventBus>();
        services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<KafkaEventBus>());
        
        services.AddScoped<IInboxStore, InboxStore>();
        services.AddSingleton<IDeadLetterQueue, KafkaDeadLetterQueue>();
        services.AddHostedService<InboxProcessor>();
        
        services.AddHostedService<KafkaConsumer>();
        services.AddScoped<IKafkaConsumer>(sp => 
            sp.GetServices<IHostedService>().OfType<KafkaConsumer>().First());
        
        services.AddHostedService<OutboxPublisher>();
        
        return services;
    }
}