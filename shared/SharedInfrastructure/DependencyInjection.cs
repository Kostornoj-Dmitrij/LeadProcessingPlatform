using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharedInfrastructure.EventBus;
using SharedInfrastructure.Inbox;
using SharedInfrastructure.Outbox;
using SharedKernel.Base;

namespace SharedInfrastructure;

/// <summary>
/// Регистрация зависимостей общей инфраструктуры
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddSharedInfrastructure<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        IEnumerable<string> topics)
        where TContext : DbContext, IUnitOfWork
    {
        services.AddSingleton(sp => new KafkaEventBus(configuration, sp.GetRequiredService<ILogger<KafkaEventBus>>()));
        services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<KafkaEventBus>());

        services.AddSingleton(sp => new KafkaConsumer(
            configuration,
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<ILogger<KafkaConsumer>>(),
            serviceName,
            topics));

        services.AddHostedService(sp => sp.GetRequiredService<KafkaConsumer>());

        services.AddScoped<IKafkaConsumer>(sp => sp.GetRequiredService<KafkaConsumer>());

        services.AddScoped<InboxStore<TContext>>();
        services.AddScoped<IInboxStore>(sp => sp.GetRequiredService<InboxStore<TContext>>());

        services.AddHostedService<InboxProcessor<InboxStore<TContext>>>();

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<TContext>());

        services.AddSingleton<IDeadLetterQueue, KafkaDeadLetterQueue>();

        services.AddHostedService<OutboxPublisher<TContext>>();

        return services;
    }
}