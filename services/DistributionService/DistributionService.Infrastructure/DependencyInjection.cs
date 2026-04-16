using AvroSchemas;
using DistributionService.Application.Common.Interfaces;
using DistributionService.Infrastructure.Background;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DistributionService.Infrastructure.Clients;
using DistributionService.Infrastructure.Data;
using DistributionService.Infrastructure.Options;
using DistributionService.Infrastructure.Outbox;
using SharedInfrastructure;
using SharedInfrastructure.Outbox;

namespace DistributionService.Infrastructure;

/// <summary>
/// Регистрация зависимостей слоя Infrastructure
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IDomainEventToOutboxConverter, DomainEventToOutboxConverter>();

        services.AddHostedService<DistributionProcessor>();

        services.Configure<DistributionOptions>(configuration.GetSection("Distribution"));

        services.AddHttpClient<IDistributionTargetClient, DistributionTargetClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<DistributionOptions>>().Value;
            if (!string.IsNullOrEmpty(options.TargetApiUrl))
            {
                client.BaseAddress = new Uri(options.TargetApiUrl);
            }
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        var topics = new[]
        {
            KafkaTopics.LeadEvents
        };

        services.AddSharedInfrastructure<ApplicationDbContext>(
            configuration,
            "distribution-service",
            topics);

        return services;
    }
}