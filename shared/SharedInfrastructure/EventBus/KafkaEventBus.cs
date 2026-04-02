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

            var leadIdProp = avroEvent.GetType().GetProperty("LeadId");
            string? leadIdValue = null;
            if (leadIdProp != null && leadIdProp.GetValue(avroEvent) is Guid leadId)
            {
                leadIdValue = leadId.ToString();
                message.Headers.Add("lead-id", Encoding.UTF8.GetBytes(leadIdValue));
            }

            var currentActivity = Activity.Current;
            if (currentActivity != null)
            {
                var traceparent = $"00-{currentActivity.TraceId}-{currentActivity.SpanId}-01";
                message.Headers.Add("traceparent", Encoding.UTF8.GetBytes(traceparent));

                foreach (var item in currentActivity.Baggage)
                {
                    if (item.Value != null)
                        message.Headers.Add($"baggage-{item.Key}", Encoding.UTF8.GetBytes(item.Value));
                }
            }

            using var produceActivity = TelemetryConstants.ActivitySource.StartProducerSpan(
                    TelemetrySpanNames.KafkaProduce,
                    typeof(TEvent).Name.Replace("Event", ""))!
                .AddTags(
                    (TelemetryAttributes.EventType, typeof(TEvent).Name),
                    (TelemetryAttributes.KafkaTopic, topic),
                    (TelemetryAttributes.ServiceName, "LeadService"),
                    (TelemetryAttributes.KafkaMessagingOperation, "publish"),
                    (TelemetryAttributes.LeadId, leadIdValue));
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