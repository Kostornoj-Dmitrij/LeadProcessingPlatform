using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ScoringService.Domain.Services;
using ScoringService.Infrastructure.Background;
using ScoringService.Infrastructure.Data;
using ScoringService.Infrastructure.Outbox;
using SharedInfrastructure;
using SharedInfrastructure.Outbox;

namespace ScoringService.Infrastructure;

/// <summary>
/// Регистрация зависимостей слоя Infrastructure
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IDomainEventToOutboxConverter, DomainEventToOutboxConverter>();

        services.AddScoped<IRuleEvaluator, RuleEvaluator>();
        services.AddHostedService<ScoringProcessor>();

        var topics = new[]
        {
            "lead-events",
            "saga-events",
            "distribution-events",
            "enrichment-events"
        };

        services.AddSharedInfrastructure<ApplicationDbContext>(
            configuration, 
            "scoring-service", 
            topics);

        return services;
    }
}