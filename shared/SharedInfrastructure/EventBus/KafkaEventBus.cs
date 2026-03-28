using System.Diagnostics;
using System.Text;
using AvroSchemas;
using AvroSchemas.Messages.Base;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharedInfrastructure.Serialization;

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

    public KafkaEventBus(IConfiguration configuration, IServiceProvider serviceProvider, ILogger<KafkaEventBus> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        var bootstrapServers = configuration["Kafka:BootstrapServers"];
        var schemaRegistryUrl = configuration["Kafka:SchemaRegistryUrl"];

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
            var topic = KafkaTopics.GetTopic(typeof(TEvent));
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
                    { "event-type", Encoding.UTF8.GetBytes(typeof(TEvent).AssemblyQualifiedName!) },
                    { "event-id", Encoding.UTF8.GetBytes(avroEvent.EventId.ToString()) },
                    { "content-type", "application/avro"u8.ToArray() },
                    { "timestamp", Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")) }
                }
            };

            var currentActivity = Activity.Current;
            if (currentActivity?.Id != null)
            {
                message.Headers.Add("trace-id", Encoding.UTF8.GetBytes(currentActivity.Id));
                if (!string.IsNullOrEmpty(currentActivity.TraceStateString))
                    message.Headers.Add("tracestate", Encoding.UTF8.GetBytes(currentActivity.TraceStateString));
            }

            await _producer.ProduceAsync(topic, message, cancellationToken);

            _logger.LogDebug(
                "Published event {EventType} with ID {EventId} to topic {Topic} using subject {Subject}",
                typeof(TEvent).Name,
                avroEvent.EventId,
                topic,
                subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType}", typeof(TEvent).Name);
            throw;
        }
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