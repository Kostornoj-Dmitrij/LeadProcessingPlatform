using System.Diagnostics;
using System.Text;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharedInfrastructure.Inbox;

namespace SharedInfrastructure.EventBus;

/// <summary>
/// Реализация потребителя Kafka
/// </summary>
public class KafkaConsumer : BackgroundService, IKafkaConsumer
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KafkaConsumer> _logger;
    private readonly IProducer<string, string> _dlqProducer;
    private readonly string _dlqTopic;
    private readonly int _maxRetryAttempts = 3;
    private bool _isRunning;
    private readonly string _serviceName;
    private readonly ActivitySource _activitySource;
    private readonly IEnumerable<string> _topics;

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
        _activitySource = new ActivitySource($"{serviceName}.KafkaConsumer");

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"],
            GroupId = configuration["Kafka:GroupId"] ?? $"{serviceName}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
            EnableAutoOffsetStore = false,
            MaxPollIntervalMs = 300000,
            SessionTimeoutMs = 30000,
            HeartbeatIntervalMs = 3000,
            PartitionAssignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky
        };

        _consumer = new ConsumerBuilder<string, string>(consumerConfig)
            .SetErrorHandler((_, error) => _logger.LogError("Kafka consumer error for {ServiceName}: {Error}", _serviceName, error.Reason))
            .Build();

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"],
            EnableIdempotence = true,
            Acks = Acks.All
        };
        _dlqProducer = new ProducerBuilder<string, string>(producerConfig).Build();
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
        ConsumeResult<string, string> consumeResult,
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
        ConsumeResult<string, string> consumeResult,
        CancellationToken cancellationToken)
    {
        if (!consumeResult.Message.Headers.TryGetLastBytes("event-type", out var eventTypeBytes) ||
            !consumeResult.Message.Headers.TryGetLastBytes("event-id", out var eventIdBytes))
        {
            throw new InvalidOperationException("Missing required headers");
        }

        var eventTypeName = Encoding.UTF8.GetString(eventTypeBytes);
        var eventId = Encoding.UTF8.GetString(eventIdBytes);

        string? traceId = null;
        ActivityContext parentContext = default;
        if (consumeResult.Message.Headers.TryGetLastBytes("trace-id", out var traceIdBytes))
        {
            traceId = Encoding.UTF8.GetString(traceIdBytes);
            if (ActivityContext.TryParse(traceId, null, out var parsedContext))
                parentContext = parsedContext;
        }

        using var activity = _activitySource.StartActivity(
            $"Kafka Consumer {eventTypeName}",
            ActivityKind.Consumer,
            parentContext: parentContext);

        if (activity != null)
        {
            activity.SetTag("messaging.system", "kafka");
            activity.SetTag("messaging.destination", consumeResult.Topic);
            activity.SetTag("messaging.message_id", eventId);
            activity.SetTag("messaging.kafka.partition", consumeResult.Partition.Value);
            activity.SetTag("messaging.kafka.offset", consumeResult.Offset.Value);
            activity.SetTag("event.type", eventTypeName);
            activity.SetTag("service.name", _serviceName);
        }

        var eventType = Type.GetType(eventTypeName);
        if (eventType == null && eventTypeName.Contains(','))
        {
            var className = eventTypeName.Split(',')[0].Trim();
            var lastDot = className.LastIndexOf('.');
            if (lastDot > 0)
            {
                var simpleName = className.Substring(lastDot + 1);
                eventType = Type.GetType($"IntegrationEvents.{simpleName}, IntegrationEvents");
            }
        }

        if (eventType == null)
            throw new InvalidOperationException($"Unknown event type: {eventTypeName}");

        using var scope = _scopeFactory.CreateScope();
        var inboxStore = scope.ServiceProvider.GetRequiredService<IInboxStore>();

        var added = await inboxStore.TryAddAsync(
            messageId: eventId,
            topic: consumeResult.Topic,
            key: consumeResult.Message.Key,
            eventType: eventType.AssemblyQualifiedName!,
            payload: consumeResult.Message.Value,
            traceId: traceId,
            cancellationToken: cancellationToken);

        if (!added)
            _logger.LogDebug("[{ServiceName}] Message {EventId} already in inbox, skipping", _serviceName, eventId);
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
        ConsumeResult<string, string> consumeResult,
        Exception exception)
    {
        var deadLetterMessage = new Message<string, string>
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