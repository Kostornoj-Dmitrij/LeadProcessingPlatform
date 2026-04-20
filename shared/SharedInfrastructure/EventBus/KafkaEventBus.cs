using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using AvroSchemas;
using AvroSchemas.Messages.Base;
using AvroSchemas.Naming;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharedHosting.Constants;
using SharedHosting.Telemetry;
using SharedInfrastructure.Constants;
using SharedInfrastructure.Serialization;
using SharedInfrastructure.Telemetry;

namespace SharedInfrastructure.EventBus;

/// <summary>
/// Реализация публикации событий в Kafka
/// </summary>
public class KafkaEventBus : IEventBus
{
    private readonly IProducer<string, byte[]> _producer;
    private readonly ISchemaRegistryClient _schemaRegistry;
    private readonly ILogger<KafkaEventBus> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _serviceName;

    private static readonly ConcurrentDictionary<Type, Func<object, CancellationToken, Task>> PublishDelegates = new();

    public KafkaEventBus(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<KafkaEventBus> logger,
        string serviceName)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _serviceName = serviceName;

        var bootstrapServers = configuration[ConfigurationKeys.KafkaBootstrapServers];
        var schemaRegistryUrl = configuration[ConfigurationKeys.KafkaSchemaRegistryUrl];

        if (string.IsNullOrEmpty(schemaRegistryUrl))
            throw new InvalidOperationException("SchemaRegistryUrl is not configured");

        var schemaRegistryConfig = new SchemaRegistryConfig { Url = schemaRegistryUrl };
        _schemaRegistry = new CachedSchemaRegistryClient(schemaRegistryConfig);

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            EnableIdempotence = true,
            Acks = Acks.All,
            MessageSendMaxRetries = 3,
            RetryBackoffMs = 1000,
            EnableDeliveryReports = true
        };

        _producer = new ProducerBuilder<string, byte[]>(producerConfig)
            .SetErrorHandler((_, error) => _logger.LogError("Kafka producer error: {Error}", error.Reason))
            .Build();
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class, IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (@event is not IntegrationEventAvro avroEvent)
            throw new ArgumentException($"Event must be of type {nameof(IntegrationEventAvro)}", nameof(@event));

        try
        {
            var naming = _serviceProvider.GetRequiredService<INamingConvention>();
            var baseTopic = KafkaTopics.GetBaseTopic<TEvent>();
            var topic = naming.GetTopicName(baseTopic);
            var subject = $"{topic}-{typeof(TEvent).Name}";
            var serializerType = typeof(AvroSerializer<>).MakeGenericType(avroEvent.GetType());
            var serializer = _serviceProvider.GetRequiredService(serializerType);
            var serializeMethod = serializerType.GetMethod("SerializeAsync");

            var messageValue = await (Task<byte[]>)serializeMethod!.Invoke(serializer,
                [avroEvent, new SerializationContext(MessageComponentType.Value, subject)])!;

            var message = new Message<string, byte[]>
            {
                Key = GetMessageKey(avroEvent),
                Value = messageValue,
                Headers = new Headers
                {
                    { KafkaHeaderKeys.EventType, Encoding.UTF8.GetBytes(typeof(TEvent).AssemblyQualifiedName!) },
                    { KafkaHeaderKeys.EventId, Encoding.UTF8.GetBytes(avroEvent.EventId.ToString()) },
                    { KafkaHeaderKeys.ContentType, KafkaHeaderValues.ContentTypeAvroBytes },
                    { KafkaHeaderKeys.Timestamp, Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")) }
                }
            };

            var leadIdProp = avroEvent.GetType().GetProperty("LeadId");
            string? leadIdValue = null;
            if (leadIdProp != null && leadIdProp.GetValue(avroEvent) is Guid leadId)
            {
                leadIdValue = leadId.ToString();
                message.Headers.Add(KafkaHeaderKeys.LeadId, Encoding.UTF8.GetBytes(leadIdValue));
            }

            var traceParent = TraceContextCarrier.TraceParent;
            if (!string.IsNullOrEmpty(traceParent))
            {
                message.Headers.Add(KafkaHeaderKeys.TraceParent, Encoding.UTF8.GetBytes(traceParent));
            }

            foreach (var kv in TraceContextCarrier.GetBaggage())
            {
                message.Headers.Add($"{KafkaHeaderKeys.BaggagePrefix}{kv.Key}", Encoding.UTF8.GetBytes(kv.Value));
            }

            if (TelemetryConstants.ActivitySource != null)
            {
                using var activity = ActivityBuilder.RestoreAndCreateActivity(
                        $"{TelemetrySpanNames.KafkaProduce} {typeof(TEvent).Name.Replace("Event", "")}",
                        traceParent,
                        ActivityKind.Producer)
                    .WithKafkaProducerTags(
                        typeof(TEvent).Name,
                        topic,
                        _serviceName,
                        leadIdValue);
            }

            TelemetryMetrics.KafkaMessagesPublished.Add(1, new TagList
            {
                { "topic", topic },
                { "event_type", typeof(TEvent).Name }
            });

            await _producer.ProduceAsync(topic, message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType}", typeof(TEvent).Name);
            throw;
        }
    }

    public Task PublishAsync(object @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var eventType = @event.GetType();
        var publisher = PublishDelegates.GetOrAdd(eventType, type =>
        {
            var method = typeof(KafkaEventBus)
                .GetMethod(nameof(PublishAsync), 1, [type, typeof(CancellationToken)]);

            if (method == null)
            {
                method = typeof(KafkaEventBus)
                    .GetMethods()
                    .FirstOrDefault(m => m.Name == nameof(PublishAsync) 
                                         && m.IsGenericMethod 
                                         && m.GetGenericArguments().Length == 1);
            }

            if (method == null)
                throw new InvalidOperationException($"PublishAsync<{type.Name}> not found");

            var genericMethod = method.MakeGenericMethod(type);

            return (evt, ct) => (Task)genericMethod.Invoke(this, [evt, ct])!;
        });

        return publisher(@event, cancellationToken);
    }

    private string GetMessageKey(IntegrationEventAvro @event)
    {
        var leadIdProp = @event.GetType().GetProperty("LeadId");
        if (leadIdProp != null && leadIdProp.GetValue(@event) is Guid leadId)
            return leadId.ToString();

        return Guid.NewGuid().ToString();
    }

    public void Dispose()
    {
        _producer.Dispose();
        _schemaRegistry.Dispose();
    }
}