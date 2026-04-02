using System.Diagnostics;
using System.Text;
using AvroSchemas;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharedInfrastructure.Inbox;
using SharedInfrastructure.Serialization;
using SharedInfrastructure.Telemetry;

namespace SharedInfrastructure.EventBus;

/// <summary>
/// Реализация потребителя Kafka
/// </summary>
public class KafkaConsumer : BackgroundService, IKafkaConsumer
{
    private readonly IConsumer<string, byte[]> _consumer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KafkaConsumer> _logger;
    private readonly IProducer<string, byte[]> _dlqProducer;
    private readonly string _dlqTopic;
    private readonly int _maxRetryAttempts = 3;
    private bool _isRunning;
    private readonly string _serviceName;
    private readonly IEnumerable<string> _topics;
    private readonly Dictionary<string, Type> _eventTypeCache = new();

    public KafkaConsumer(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        ILogger<KafkaConsumer> logger,
        string serviceName,
        IEnumerable<string> topics)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _serviceName = serviceName;
        _topics = topics;

        var bootstrapServers = configuration["Kafka:BootstrapServers"];
        var schemaRegistryUrl = configuration["Kafka:SchemaRegistryUrl"];

        if (string.IsNullOrEmpty(schemaRegistryUrl))
            throw new InvalidOperationException("SchemaRegistryUrl is not configured");

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = configuration["Kafka:GroupId"] ?? $"{serviceName}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
            EnableAutoOffsetStore = false,
            MaxPollIntervalMs = 300000,
            SessionTimeoutMs = 30000,
            HeartbeatIntervalMs = 3000,
            PartitionAssignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky
        };

        _consumer = new ConsumerBuilder<string, byte[]>(consumerConfig)
            .SetKeyDeserializer(Deserializers.Utf8)
            .SetValueDeserializer(Deserializers.ByteArray)
            .SetErrorHandler((_, error) => _logger.LogError("Kafka consumer error for {ServiceName}: {Error}", _serviceName, error.Reason))
            .Build();

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            EnableIdempotence = true,
            Acks = Acks.All
        };
        _dlqProducer = new ProducerBuilder<string, byte[]>(producerConfig).Build();
        _dlqTopic = configuration["Kafka:DlqTopic"] ?? $"{serviceName}-dlq";
    }

    public bool IsRunning => _isRunning;

    public void Subscribe(IEnumerable<string> topics)
    {
        var list = topics.ToList();
        _consumer.Subscribe(list);
        _logger.LogInformation("[{ServiceName}] Subscribed to topics: {Topics}", _serviceName, string.Join(", ", list));
    }

    public void Unsubscribe()
    {
        _consumer.Unsubscribe();
        _logger.LogInformation("[{ServiceName}] Unsubscribed from all topics", _serviceName);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[{ServiceName}] Kafka Consumer started", _serviceName);
        _isRunning = true;

        Subscribe(_topics);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(stoppingToken);
                    if (consumeResult == null || consumeResult.IsPartitionEOF)
                        continue;

                    await ProcessMessageWithRetryAsync(consumeResult, stoppingToken);
                    _consumer.StoreOffset(consumeResult);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "[{ServiceName}] Consume error: {Error}", _serviceName, ex.Error.Reason);
                    if (ex.Error.IsFatal)
                        throw;
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[{ServiceName}] Kafka Consumer stopped", _serviceName);
        }
        finally
        {
            _isRunning = false;
            _consumer.Close();
        }
    }

    private async Task ProcessMessageWithRetryAsync(
        ConsumeResult<string, byte[]> consumeResult,
        CancellationToken cancellationToken)
    {
        int attempt = 0;
        while (attempt < _maxRetryAttempts)
        {
            try
            {
                await ProcessMessageAsync(consumeResult, cancellationToken);
                return;
            }
            catch (Exception ex) when (IsTransientError(ex))
            {
                attempt++;
                _logger.LogWarning(ex, 
                    "[{ServiceName}] Transient error processing message (attempt {Attempt}/{MaxAttempts}), Topic: {Topic}, Offset: {Offset}", 
                    _serviceName, attempt, _maxRetryAttempts, consumeResult.Topic, consumeResult.Offset);

                if (attempt >= _maxRetryAttempts)
                {
                    _logger.LogError(ex, 
                        "[{ServiceName}] Max retry attempts reached. Moving to DLQ. Topic: {Topic}, Offset: {Offset}", 
                        _serviceName, consumeResult.Topic, consumeResult.Offset);
                    await MoveToDeadLetterQueueAsync(consumeResult, ex);
                    return;
                }

                var delay = TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100);
                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "[{ServiceName}] Non-transient error. Moving to DLQ. Topic: {Topic}, Offset: {Offset}", 
                    _serviceName, consumeResult.Topic, consumeResult.Offset);
                await MoveToDeadLetterQueueAsync(consumeResult, ex);
                return;
            }
        }
    }

    private async Task ProcessMessageAsync(
        ConsumeResult<string, byte[]> consumeResult,
        CancellationToken cancellationToken)
    {
        if (!consumeResult.Message.Headers.TryGetLastBytes("event-type", out var eventTypeBytes))
            throw new InvalidOperationException("Missing event-type header");

        var eventTypeName = Encoding.UTF8.GetString(eventTypeBytes);
        var eventType = GetEventType(eventTypeName);
        if (eventType == null)
            throw new InvalidOperationException($"Unknown event type: {eventTypeName}");

        if (!consumeResult.Message.Headers.TryGetLastBytes("event-id", out var eventIdBytes))
            throw new InvalidOperationException("Missing event-id header");
        var eventId = Encoding.UTF8.GetString(eventIdBytes);

        string? traceParent = null;

        if (consumeResult.Message.Headers.TryGetLastBytes("traceparent", out var traceParentBytes))
        {
            traceParent = Encoding.UTF8.GetString(traceParentBytes);
            if (consumeResult.Message.Headers.TryGetLastBytes("tracestate", out var traceStateBytes))
            {
                Encoding.UTF8.GetString(traceStateBytes);
            }
        }

        string leadId = ExtractLeadIdFromMessage(consumeResult.Message);
        if (!string.IsNullOrEmpty(leadId))
        {
            TelemetryContext.SetBaggage(TelemetryBaggageKeys.LeadId, leadId);
            TelemetryContext.SetBaggage(TelemetryBaggageKeys.BusinessProcess, "LeadProcessing");
        }

        string spanName = $"{TelemetrySpanNames.KafkaConsume} {GetSimpleTypeName(eventTypeName)}";

        using var activity = TelemetryRestorer.RestoreAndStartActivity(
                TelemetryConstants.ActivitySource,
                spanName,
                traceParent,
                ActivityKind.Consumer)!
            .AddTags(
                (TelemetryAttributes.EventType, eventTypeName),
                (TelemetryAttributes.EventName, GetSimpleTypeName(eventTypeName)),
                (TelemetryAttributes.EventId, eventId),
                (TelemetryAttributes.KafkaTopic, consumeResult.Topic),
                (TelemetryAttributes.KafkaPartition, consumeResult.Partition.Value),
                (TelemetryAttributes.KafkaOffset, consumeResult.Offset.Value),
                (TelemetryAttributes.KafkaConsumerGroup, _consumer.MemberId),
                (TelemetryAttributes.ServiceName, _serviceName),
                (TelemetryAttributes.LeadId, leadId),
                (TelemetryAttributes.KafkaMessagingSystem, "kafka"),
                (TelemetryAttributes.KafkaMessagingDestination, consumeResult.Topic),
                (TelemetryAttributes.KafkaMessagingMessageId, eventId),
                (TelemetryAttributes.KafkaMessagingOperation, "receive"),
                (TelemetryAttributes.ProcessingStep, "kafka_consume"));

        foreach (var header in consumeResult.Message.Headers)
        {
            if (header.Key.StartsWith("baggage-"))
            {
                var key = header.Key.Substring(8);
                var value = Encoding.UTF8.GetString(header.GetValueBytes());
                activity.SetBaggage(key, value);
            }
        }

        var traceIdToStore = activity.TraceId.ToString();

        var deserializerType = typeof(AvroDeserializer<>).MakeGenericType(eventType);

        using var scope = _scopeFactory.CreateScope();
        var deserializer = scope.ServiceProvider.GetRequiredService(deserializerType);

        dynamic dynamicDeserializer = deserializer;
        var data = new ReadOnlyMemory<byte>(consumeResult.Message.Value);
        var context = new SerializationContext(MessageComponentType.Value, consumeResult.Topic);

        var avroEvent = await dynamicDeserializer.DeserializeAsync(data, false, context);

        var integrationEvent = (IIntegrationEvent)avroEvent;

        var payloadJson = System.Text.Json.JsonSerializer.Serialize(integrationEvent, eventType, SharedKernel.Json.JsonDefaults.Options);

        var inboxStore = scope.ServiceProvider.GetRequiredService<IInboxStore>();

        var added = await inboxStore.TryAddAsync(
            messageId: eventId,
            topic: consumeResult.Topic,
            key: consumeResult.Message.Key,
            eventType: eventType.AssemblyQualifiedName!,
            payload: payloadJson,
            traceId: traceIdToStore,
            cancellationToken: cancellationToken);

        if (!added)
            _logger.LogDebug("[{ServiceName}] Message {EventId} already in inbox, skipping", _serviceName, eventId);
    }

    private Type? GetEventType(string eventTypeName)
    {
        if (_eventTypeCache.TryGetValue(eventTypeName, out var cachedType))
            return cachedType;

        var type = Type.GetType(eventTypeName);
        if (type != null)
        {
            _eventTypeCache[eventTypeName] = type;
            return type;
        }

        return null;
    }

    private string GetSimpleTypeName(string eventTypeName)
    {
        try
        {
            var parts = eventTypeName.Split(',');
            if (parts.Length > 0)
            {
                var fullTypeName = parts[0].Trim();
                var lastDotIndex = fullTypeName.LastIndexOf('.');
                if (lastDotIndex >= 0)
                {
                    return fullTypeName.Substring(lastDotIndex + 1);
                }
                return fullTypeName;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse event type name: {EventTypeName}", eventTypeName);
        }

        var cleaned = eventTypeName.Split(',')[0];
        var lastDot = cleaned.LastIndexOf('.');
        if (lastDot >= 0)
        {
            return cleaned.Substring(lastDot + 1);
        }

        return eventTypeName;
    }

    private string ExtractLeadIdFromMessage(Message<string, byte[]> message)
    {
        if (message.Headers.TryGetLastBytes("lead-id", out var leadIdBytes))
        {
            return Encoding.UTF8.GetString(leadIdBytes);
        }

        if (!string.IsNullOrEmpty(message.Key) && Guid.TryParse(message.Key, out _))
        {
            return message.Key;
        }

        return string.Empty;
    }

    private bool IsTransientError(Exception ex)
    {
        return ex switch
        {
            TimeoutException => true,
            Npgsql.NpgsqlException { IsTransient: true } => true,
            DbUpdateException dbEx when dbEx.InnerException?.Message.Contains("deadlock") == true => true,
            _ => false
        };
    }

    private async Task MoveToDeadLetterQueueAsync(
        ConsumeResult<string, byte[]> consumeResult,
        Exception exception)
    {
        var deadLetterMessage = new Message<string, byte[]>
        {
            Key = consumeResult.Message.Key,
            Value = consumeResult.Message.Value,
            Headers = new Headers
            {
                { "original-topic", Encoding.UTF8.GetBytes(consumeResult.Topic) },
                { "original-partition", Encoding.UTF8.GetBytes(consumeResult.Partition.ToString()) },
                { "original-offset", Encoding.UTF8.GetBytes(consumeResult.Offset.ToString()) },
                { "error-message", Encoding.UTF8.GetBytes(exception.Message) },
                { "error-type", Encoding.UTF8.GetBytes(exception.GetType().Name) },
                { "timestamp", Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")) },
                { "source", "kafka-consumer"u8.ToArray() },
                { "service-name", Encoding.UTF8.GetBytes(_serviceName) }
            }
        };

        if (consumeResult.Message.Headers != null)
        {
            foreach (var header in consumeResult.Message.Headers)
            {
                deadLetterMessage.Headers.Add(header.Key, header.GetValueBytes());
            }
        }

        await _dlqProducer.ProduceAsync(_dlqTopic, deadLetterMessage);

        _logger.LogWarning(
            "[{ServiceName}] Message moved to DLQ. Original topic: {Topic}, Offset: {Offset}, Error: {Error}",
            _serviceName,
            consumeResult.Topic,
            consumeResult.Offset,
            exception.Message);
    }

    public override void Dispose()
    {
        base.Dispose();
        _consumer.Dispose();
        _dlqProducer.Dispose();
    }
}