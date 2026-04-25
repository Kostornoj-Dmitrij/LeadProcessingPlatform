using System.Diagnostics;
using System.Text;
using System.Threading.Channels;
using AvroSchemas;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharedHosting.Constants;
using SharedHosting.Extensions;
using SharedHosting.Options;
using SharedHosting.Telemetry;
using SharedInfrastructure.Constants;
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
    private readonly Channel<ConsumeResult<string, byte[]>> _messageChannel;
    private readonly int _parallelismDegree;

    public KafkaConsumer(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        ILogger<KafkaConsumer> logger,
        string serviceName,
        IEnumerable<string> topics,
        string dlqTopic)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _serviceName = serviceName;
        _topics = topics;
        _dlqTopic = dlqTopic;

        var bootstrapServers = configuration[ConfigurationKeys.KafkaBootstrapServers];
        var schemaRegistryUrl = configuration[ConfigurationKeys.KafkaSchemaRegistryUrl];
        var kafkaOptions = configuration.GetSection(KafkaOptions.SectionName).Get<KafkaOptions>();

        if (string.IsNullOrEmpty(schemaRegistryUrl))
            throw new InvalidOperationException("SchemaRegistryUrl is not configured");

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = configuration[ConfigurationKeys.KafkaGroupId] ?? $"{serviceName}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false,
            MaxPollIntervalMs = kafkaOptions?.ConsumerMaxPollIntervalMs ?? 300000,
            SessionTimeoutMs = kafkaOptions?.ConsumerSessionTimeoutMs ?? 30000,
            HeartbeatIntervalMs = 3000,
            MaxPartitionFetchBytes = 1048576,
            FetchMinBytes = kafkaOptions?.ConsumerFetchMinBytes ?? 1024,
            FetchWaitMaxMs = kafkaOptions?.ConsumerFetchMaxWaitMs ?? 50,
            PartitionAssignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky
        };
        consumerConfig.ApplySaslConfig(kafkaOptions);

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
        producerConfig.ApplySaslConfig(kafkaOptions);

        _dlqProducer = new ProducerBuilder<string, byte[]>(producerConfig).Build();

        _messageChannel = Channel.CreateUnbounded<ConsumeResult<string, byte[]>>(
            new UnboundedChannelOptions { SingleReader = false });
        _parallelismDegree = Math.Max(1, Environment.ProcessorCount / 2);
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

        var workers = Enumerable.Range(0, _parallelismDegree)
            .Select(_ => Task.Run(() => ProcessMessagesAsync(stoppingToken), stoppingToken))
            .ToArray();

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(TimeSpan.FromMilliseconds(100));
                    if (consumeResult == null || consumeResult.IsPartitionEOF)
                        continue;

                    await _messageChannel.Writer.WriteAsync(consumeResult, stoppingToken);
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
            _messageChannel.Writer.Complete();
            await Task.WhenAll(workers);

            try
            {
                _consumer.Commit();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[{ServiceName}] Error committing offset during shutdown", _serviceName);
            }

            _isRunning = false;
            _consumer.Close();
        }
    }

    private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
    {
        await foreach (var consumeResult in _messageChannel.Reader.ReadAllAsync(cancellationToken))
        {
            var success = await ProcessMessageWithRetryAsync(consumeResult, cancellationToken);

            if (success)
                _consumer.StoreOffset(consumeResult);
        }
    }

    private async Task<bool> ProcessMessageWithRetryAsync(
        ConsumeResult<string, byte[]> consumeResult,
        CancellationToken cancellationToken)
    {
        var attempt = 0;
        while (attempt < _maxRetryAttempts)
        {
            try
            {
                await ProcessMessageAsync(consumeResult, cancellationToken);
                return true;
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
                    return true;
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
                return true;
            }
        }

        return false;
    }

    private async Task ProcessMessageAsync(
        ConsumeResult<string, byte[]> consumeResult,
        CancellationToken cancellationToken)
    {
        if (!consumeResult.Message.Headers.TryGetLastBytes(KafkaHeaderKeys.EventType, out var eventTypeBytes))
            throw new InvalidOperationException("Missing event-type header");

        var eventTypeName = Encoding.UTF8.GetString(eventTypeBytes);
        var eventType = EventTypeRegistry.GetType(eventTypeName);
        if (eventType == null)
            throw new InvalidOperationException($"Unknown event type: {eventTypeName}");

        if (!consumeResult.Message.Headers.TryGetLastBytes(KafkaHeaderKeys.EventId, out var eventIdBytes))
            throw new InvalidOperationException("Missing event-id header");
        var eventId = Encoding.UTF8.GetString(eventIdBytes);

        string? traceParent = null;

        if (consumeResult.Message.Headers.TryGetLastBytes(KafkaHeaderKeys.TraceParent, out var traceParentBytes))
        {
            traceParent = Encoding.UTF8.GetString(traceParentBytes);
            TraceContextCarrier.TraceParent = traceParent;
        }

        foreach (var header in consumeResult.Message.Headers)
        {
            if (header.Key.StartsWith(KafkaHeaderKeys.BaggagePrefix))
            {
                var key = header.Key.Substring(KafkaHeaderKeys.BaggagePrefix.Length);
                var value = Encoding.UTF8.GetString(header.GetValueBytes());
                TraceContextCarrier.SetBaggage(key, value);
            }
        }

        var leadId = ExtractLeadIdFromMessage(consumeResult.Message);
        if (!string.IsNullOrEmpty(leadId))
        {
            TelemetryContext.SetBaggage(TelemetryBaggageKeys.LeadId, leadId);
            TelemetryContext.SetBaggage(TelemetryBaggageKeys.BusinessProcess, TelemetryBaggageKeys.LeadProcessing);
        }

        var spanName = $"{TelemetrySpanNames.KafkaConsume} {GetSimpleTypeName(eventTypeName)}";
        string? traceIdToStore;

        if (TelemetryConstants.ActivitySource != null)
        {
            using var activity = ActivityBuilder.RestoreAndCreateActivity(
                    spanName,
                    traceParent,
                    ActivityKind.Consumer)
                .WithKafkaConsumerTags(
                    eventTypeName,
                    GetSimpleTypeName(eventTypeName),
                    eventId,
                    consumeResult.Topic,
                    consumeResult.Partition.Value,
                    consumeResult.Offset.Value,
                    _consumer.MemberId,
                    _serviceName,
                    leadId);

            traceIdToStore = activity.TraceId;
        }
        else
        {
            traceIdToStore = traceParent?.Split('-')[1];
        }

        var deserializerType = typeof(AvroDeserializer<>).MakeGenericType(eventType);

        using var scope = _scopeFactory.CreateScope();
        var deserializer = scope.ServiceProvider.GetRequiredService(deserializerType);

        dynamic dynamicDeserializer = deserializer;
        var data = new ReadOnlyMemory<byte>(consumeResult.Message.Value);
        var context = new SerializationContext(MessageComponentType.Value, consumeResult.Topic);

        var avroEvent = await dynamicDeserializer.DeserializeAsync(data, false, context);

        var integrationEvent = (IIntegrationEvent)avroEvent;

        var payloadJson = System.Text.Json.JsonSerializer.Serialize(
            integrationEvent, eventType, SharedKernel.Json.JsonDefaults.Options);

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
                    return fullTypeName.Substring(lastDotIndex + 1);
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
            return cleaned.Substring(lastDot + 1);
        return eventTypeName;
    }

    private string ExtractLeadIdFromMessage(Message<string, byte[]> message)
    {
        if (message.Headers.TryGetLastBytes(KafkaHeaderKeys.LeadId, out var leadIdBytes))
            return Encoding.UTF8.GetString(leadIdBytes);

        if (!string.IsNullOrEmpty(message.Key) && Guid.TryParse(message.Key, out _))
            return message.Key;

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
                { KafkaHeaderKeys.OriginalTopic, Encoding.UTF8.GetBytes(consumeResult.Topic) },
                { KafkaHeaderKeys.OriginalPartition, Encoding.UTF8.GetBytes(consumeResult.Partition.ToString()) },
                { KafkaHeaderKeys.OriginalOffset, Encoding.UTF8.GetBytes(consumeResult.Offset.ToString()) },
                { KafkaHeaderKeys.ErrorMessage, Encoding.UTF8.GetBytes(exception.Message) },
                { KafkaHeaderKeys.ErrorType, Encoding.UTF8.GetBytes(exception.GetType().Name) },
                { KafkaHeaderKeys.Timestamp, Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")) },
                { KafkaHeaderKeys.Source, KafkaHeaderValues.SourceKafkaConsumerBytes },
                { KafkaHeaderKeys.ServiceName, Encoding.UTF8.GetBytes(_serviceName) }
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