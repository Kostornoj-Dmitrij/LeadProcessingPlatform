using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ScoringService.Application.Common.Interfaces;
using ScoringService.Infrastructure.Background;
using ScoringService.Infrastructure.Data;
using ScoringService.Infrastructure.EventBus;
using ScoringService.Infrastructure.Inbox;
using ScoringService.Infrastructure.Outbox;
using SharedKernel.Base;
using Microsoft.Extensions.Hosting;
using ScoringService.Domain.Services;

namespace ScoringService.Infrastructure;

/// <summary>
/// Регистрация зависимостей слоя Infrastructure
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
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

        services.AddScoped<IRuleEvaluator, RuleEvaluator>();
        services.AddHostedService<OutboxPublisher>();
        services.AddHostedService<ScoringProcessor>();

        return services;
    }
}