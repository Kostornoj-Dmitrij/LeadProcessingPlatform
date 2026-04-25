using AvroSchemas.Naming;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharedHosting.Constants;
using SharedInfrastructure.EventBus;
using SharedInfrastructure.Inbox;
using SharedInfrastructure.Outbox;
using SharedInfrastructure.Serialization;
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
        string[] baseTopics)
        where TContext : DbContext, IUnitOfWork
    {
        EventTypeRegistry.Initialize();

        services.AddSingleton<ISchemaRegistryClient>(_ =>
        {
            var schemaRegistryUrl = configuration[ConfigurationKeys.KafkaSchemaRegistryUrl];
            if (string.IsNullOrEmpty(schemaRegistryUrl))
                throw new InvalidOperationException($"{ConfigurationKeys.KafkaSchemaRegistryUrl} is not configured");

            var config = new SchemaRegistryConfig { Url = schemaRegistryUrl };
            return new CachedSchemaRegistryClient(config);
        });

        RegisterAvroSerializers(services);

        services.AddSingleton(sp => new KafkaEventBus(
            configuration, 
            sp, 
            sp.GetRequiredService<ILogger<KafkaEventBus>>(),
            serviceName));
        services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<KafkaEventBus>());

        services.AddSingleton(sp =>
        {
            var naming = sp.GetRequiredService<INamingConvention>();
            var topics = baseTopics.Select(naming.GetTopicName).ToList();
            var dlqTopic = naming.GetDlqTopicName(serviceName);

            return new KafkaConsumer(
                configuration,
                sp,
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<ILogger<KafkaConsumer>>(),
                serviceName,
                topics,
                dlqTopic);
        });

        services.AddHostedService(sp => sp.GetRequiredService<KafkaConsumer>());
        services.AddScoped<IKafkaConsumer>(sp => sp.GetRequiredService<KafkaConsumer>());

        services.AddScoped<InboxStore<TContext>>();
        services.AddScoped<IInboxStore>(sp => sp.GetRequiredService<InboxStore<TContext>>());
        services.AddHostedService<InboxProcessor<InboxStore<TContext>>>();

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<TContext>());

        services.AddSingleton<IDeadLetterQueue>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var logger = sp.GetRequiredService<ILogger<KafkaDeadLetterQueue>>();
            var naming = sp.GetRequiredService<INamingConvention>();
            return new KafkaDeadLetterQueue(config, logger, naming, serviceName);
        });

        services.AddHostedService<OutboxPublisher<TContext>>();

        return services;
    }

    private static void RegisterAvroSerializers(IServiceCollection services)
    {
        var avroTypes = EventTypeRegistry.AllTypes;

        foreach (var type in avroTypes)
        {
            var serializerType = typeof(AvroSerializer<>).MakeGenericType(type);
            var deserializerType = typeof(AvroDeserializer<>).MakeGenericType(type);

            services.AddSingleton(serializerType, sp =>
            {
                var schemaRegistry = sp.GetRequiredService<ISchemaRegistryClient>();
                return Activator.CreateInstance(serializerType, schemaRegistry, null)!;
            });

            services.AddSingleton(deserializerType, sp =>
            {
                var schemaRegistry = sp.GetRequiredService<ISchemaRegistryClient>();
                var loggerType = typeof(ILogger<>).MakeGenericType(deserializerType);
                var logger = sp.GetService(loggerType);
                return Activator.CreateInstance(deserializerType, schemaRegistry, logger)!;
            });

            services.AddSingleton(typeof(IAsyncSerializer<>).MakeGenericType(type), 
                sp => sp.GetRequiredService(serializerType));
            services.AddSingleton(typeof(IAsyncDeserializer<>).MakeGenericType(type), 
                sp => sp.GetRequiredService(deserializerType));
        }
    }
}