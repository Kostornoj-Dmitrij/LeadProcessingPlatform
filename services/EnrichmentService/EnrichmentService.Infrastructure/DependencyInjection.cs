using AvroSchemas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EnrichmentService.Application.Common.Interfaces;
using EnrichmentService.Infrastructure.Background;
using EnrichmentService.Infrastructure.Clients;
using EnrichmentService.Infrastructure.Data;
using EnrichmentService.Infrastructure.Outbox;
using SharedInfrastructure;
using SharedInfrastructure.Outbox;

namespace EnrichmentService.Infrastructure;

/// <summary>
/// Регистрация зависимостей слоя Infrastructure
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IDomainEventToOutboxConverter, DomainEventToOutboxConverter>();
        
        services.AddHostedService<EnrichmentProcessor>();
        
        services.AddHttpClient<IExternalEnrichmentClient, ExternalEnrichmentClient>();

        var topics = new[]
        {
            KafkaTopics.LeadEventsBase,
            KafkaTopics.SagaEventsBase,
            KafkaTopics.DistributionEventsBase
        };

        services.AddSharedInfrastructure<ApplicationDbContext>(
            configuration, 
            "enrichment-service", 
            topics);

        return services;
    }
}