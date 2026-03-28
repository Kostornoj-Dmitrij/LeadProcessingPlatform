using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Confluent.Kafka;
using SharedHosting.HealthChecks;

namespace SharedHosting.Extensions;

/// <summary>
/// Расширения для настройки Health Checks
/// </summary>
public static class HealthCheckExtensions
{
    public static IServiceCollection AddSharedHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
    {
        var healthChecks = services.AddHealthChecks();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(connectionString))
        {
            healthChecks.AddNpgSql(
                connectionString,
                name: "postgres",
                tags: ["db", "postgres"]);
        }

        var bootstrapServers = configuration["Kafka:BootstrapServers"];
        if (!string.IsNullOrEmpty(bootstrapServers))
        {
            healthChecks.AddKafka(
                new ProducerConfig { BootstrapServers = bootstrapServers },
                name: "kafka",
                tags: ["messaging", "kafka"]);
        }

        var schemaRegistryUrl = configuration["Kafka:SchemaRegistryUrl"];
        if (!string.IsNullOrEmpty(schemaRegistryUrl))
        {
            healthChecks.AddUrlGroup(
                new Uri(schemaRegistryUrl),
                name: "schema-registry",
                timeout: TimeSpan.FromSeconds(5),
                tags: ["messaging", "schema-registry"]);
        }

        healthChecks.AddCheck<ServiceHealthCheck>(serviceName, tags: ["service"]);

        return services;
    }
}