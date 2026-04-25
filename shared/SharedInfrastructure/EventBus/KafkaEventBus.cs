using System.Diagnostics;
using System.Text;
using AvroSchemas;
using AvroSchemas.Messages.Base;
using AvroSchemas.Naming;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharedHosting.Constants;
using SharedHosting.Extensions;
using SharedHosting.Options;
using SharedHosting.Telemetry;
using SharedInfrastructure.Constants;
using SharedInfrastructure.Telemetry;

namespace SharedInfrastructure.EventBus;

/// <summary>
/// Реализация публикации событий в Kafka
/// </summary>
public class KafkaEventBus : IEventBus
{
    private readonly IProducer<string, byte[]> _producer;
    private readonly ILogger<KafkaEventBus> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _serviceName;

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
        var kafkaOptions = configuration.GetSection(KafkaOptions.SectionName).Get<KafkaOptions>();

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            EnableIdempotence = true,
            Acks = Acks.All,
            MessageSendMaxRetries = 3,
            RetryBackoffMs = 1000,
            EnableDeliveryReports = true
        };
        producerConfig.ApplySaslConfig(kafkaOptions);

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

            var serializer = _serviceProvider.GetRequiredService<IAsyncSerializer<TEvent>>();
            var messageValue = await serializer.SerializeAsync(@event,
                new SerializationContext(MessageComponentType.Value, subject));

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

            var leadIdValue = LeadIdCache.GetLeadId(avroEvent);
            if (!string.IsNullOrEmpty(leadIdValue))
            {
                message.Headers.Add(KafkaHeaderKeys.LeadId, Encoding.UTF8.GetBytes(leadIdValue));
            }

            var traceParent = TraceContextCarrier.TraceParent;
            if (!string.IsNullOrEmpty(traceParent))
            {
                message.Headers.Add(KafkaHeaderKeys.TraceParent, Encoding.UTF8.GetBytes(traceParent));
            }

            foreach (var kv in TraceContextCarrier.GetBaggage())
            {
                message.Headers.Add($"{KafkaHeaderKeys.BaggagePrefix}{kv.Key}",
                    Encoding.UTF8.GetBytes(kv.Value));
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

    private static string GetMessageKey(IntegrationEventAvro @event)
    {
        var leadId = LeadIdCache.GetLeadId(@event);
        return leadId ?? Guid.NewGuid().ToString();
    }

    public void Dispose()
    {
        _producer.Dispose();
    }
}