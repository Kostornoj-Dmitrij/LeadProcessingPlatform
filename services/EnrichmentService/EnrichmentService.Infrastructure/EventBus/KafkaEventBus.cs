using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using EnrichmentService.Application.Common.Interfaces;
using IntegrationEvents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SharedKernel.Json;

namespace EnrichmentService.Infrastructure.EventBus;

/// <summary>
/// Отвечает за публикацию событий в Kafka
/// </summary>
public class KafkaEventBus : IEventBus
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventBus> _logger;
    private readonly Dictionary<Type, string> _eventToTopicMap;

    public KafkaEventBus(IConfiguration configuration, ILogger<KafkaEventBus> logger)
    {
        _logger = logger;

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"],
            EnableIdempotence = true,
            Acks = Acks.All,
            MessageSendMaxRetries = 3,
            RetryBackoffMs = 1000,
            EnableDeliveryReports = true
        };

        _producer = new ProducerBuilder<string, string>(producerConfig)
            .SetErrorHandler((_, error) => _logger.LogError("Kafka producer error: {Error}", error.Reason))
            .Build();

        _eventToTopicMap = KafkaTopics.Mappings.EventToTopic
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (@event is not IIntegrationEvent integrationEvent)
            throw new ArgumentException($"Event must implement {nameof(IIntegrationEvent)}", nameof(@event));

        try
        {
            var topic = GetTopicForEvent(integrationEvent);
            var messageValue = JsonSerializer.Serialize(@event, JsonDefaults.Options);

            var message = new Message<string, string>
            {
                Key = GetMessageKey(integrationEvent),
                Value = messageValue,
                Headers = new Headers
                {
                    { "event-type", Encoding.UTF8.GetBytes(integrationEvent.GetType().AssemblyQualifiedName!) },
                    { "event-id", Encoding.UTF8.GetBytes(integrationEvent.EventId.ToString()) },
                    { "content-type", "application/json"u8.ToArray() },
                    { "timestamp", Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")) }
                }
            };

            var currentActivity = System.Diagnostics.Activity.Current;
            if (currentActivity?.Id != null)
            {
                message.Headers.Add("trace-id", Encoding.UTF8.GetBytes(currentActivity.Id));
                if (!string.IsNullOrEmpty(currentActivity.TraceStateString))
                    message.Headers.Add("tracestate", Encoding.UTF8.GetBytes(currentActivity.TraceStateString));
                _logger.LogDebug("Added trace context to message: {TraceId}", currentActivity.Id);
            }

            var result = await _producer.ProduceAsync(topic, message, cancellationToken);

            _logger.LogDebug(
                "Published event {EventType} with ID {EventId} to topic {Topic} at partition {Partition}:{Offset}",
                integrationEvent.GetType().AssemblyQualifiedName!,
                integrationEvent.EventId,
                topic,
                result.Partition,
                result.Offset);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType} with ID {EventId}. Error: {Error}",
                integrationEvent.GetType().AssemblyQualifiedName!,
                integrationEvent.EventId,
                ex.Error.Reason);
            throw new InvalidOperationException($"Kafka publish failed: {ex.Error.Reason}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error publishing event {EventType}", integrationEvent.GetType().AssemblyQualifiedName!);
            throw;
        }
    }

    private string GetTopicForEvent(IIntegrationEvent @event)
    {
        var eventType = @event.GetType();
        if (_eventToTopicMap.TryGetValue(eventType, out var topic))
            return topic;
        throw new NotSupportedException($"No topic configured for event type {eventType.Name}");
    }

    private string GetMessageKey(IIntegrationEvent @event)
    {
        var leadIdProperty = @event.GetType().GetProperty("LeadId");
        if (leadIdProperty != null)
        {
            var leadId = leadIdProperty.GetValue(@event);
            if (leadId != null)
                return leadId.ToString()!;
        }
        return Guid.NewGuid().ToString();
    }

    public void Dispose()
    {
        _producer.Dispose();
    }
}