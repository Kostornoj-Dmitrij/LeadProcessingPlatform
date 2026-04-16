using AvroSchemas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LeadService.Application.Common.Interfaces;
using LeadService.Infrastructure.Data;
using LeadService.Infrastructure.Data.Repositories;
using LeadService.Infrastructure.Outbox;
using SharedInfrastructure;
using SharedInfrastructure.Outbox;

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
        services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();

        services.AddScoped<IDomainEventToOutboxConverter, DomainEventToOutboxConverter>();

        var topics = new[]
        {
            KafkaTopics.EnrichmentEvents,
            KafkaTopics.ScoringEvents,
            KafkaTopics.DistributionEvents,
            KafkaTopics.SagaEvents,
            KafkaTopics.LeadEvents
        };

        services.AddSharedInfrastructure<ApplicationDbContext>(
            configuration, 
            "lead-service", 
            topics);

        return services;
    }
}