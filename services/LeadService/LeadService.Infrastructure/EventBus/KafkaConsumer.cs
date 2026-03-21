using System.Diagnostics;
using System.Text;
using Confluent.Kafka;
using LeadService.Application.Common.Interfaces;
using LeadService.Domain.Constants;
using LeadService.Infrastructure.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace LeadService.Infrastructure.EventBus;

/// <summary>
/// Отвечает за потребление сообщений из Kafka и сохранение их в Inbox
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
    private static readonly ActivitySource ActivitySource = new("LeadService.KafkaConsumer");

    public KafkaConsumer(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        ILogger<KafkaConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"],
            GroupId = configuration["Kafka:GroupId"] ?? "lead-service",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
            EnableAutoOffsetStore = false,
            MaxPollIntervalMs = 300000,
            SessionTimeoutMs = 30000,
            HeartbeatIntervalMs = 3000,
            PartitionAssignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky
        };

        _consumer = new ConsumerBuilder<string, string>(consumerConfig)
            .SetErrorHandler((_, error) => 
                _logger.LogError("Kafka consumer error: {Error}", error.Reason))
            .Build();

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"],
            EnableIdempotence = true,
            Acks = Acks.All
        };
        _dlqProducer = new ProducerBuilder<string, string>(producerConfig).Build();
        _dlqTopic = configuration["Kafka:DlqTopic"] ?? "lead-service-dlq";

        _isRunning = false;
    }

    public bool IsRunning => _isRunning;

    public void Subscribe(IEnumerable<string> topics)
    {
        var enumerable = topics.ToList();
        _consumer.Subscribe(enumerable);
        _logger.LogInformation("Subscribed to topics: {Topics}", string.Join(", ", enumerable));
    }

    public void Unsubscribe()
    {
        _consumer.Unsubscribe();
        _logger.LogInformation("Unsubscribed from all topics");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Kafka Consumer started");
        _isRunning = true;

        var topics = new[]
        {
            "enrichment-events",
            "scoring-events",
            "distribution-events",
            "saga-events"
        };
        Subscribe(topics);
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
                    _logger.LogError(ex, "Consume error: {Error}", ex.Error.Reason);

                    if (ex.Error.IsFatal)
                    {
                        throw;
                    }

                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Kafka Consumer stopped");
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
                    "Transient error processing message (attempt {Attempt}/{MaxAttempts})", 
                    attempt, _maxRetryAttempts);

                if (attempt >= _maxRetryAttempts)
                {
                    _logger.LogError(ex, 
                        "Max retry attempts reached for message. Moving to DLQ. Topic: {Topic}, Offset: {Offset}", 
                        consumeResult.Topic, consumeResult.Offset);

                    await MoveToDeadLetterQueueAsync(consumeResult, 
                        new Exception($"Max retry attempts ({_maxRetryAttempts}) exceeded. Last error: {ex.Message}"));
                    return;
                }

                var delay = TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100);
                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Non-transient error processing message. Moving to DLQ. Topic: {Topic}, Offset: {Offset}", 
                    consumeResult.Topic, consumeResult.Offset);

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
        string? traceState = null;
        ActivityContext parentContext = default;

        if (consumeResult.Message.Headers.TryGetLastBytes("trace-id", out var traceIdBytes))
        {
            traceId = Encoding.UTF8.GetString(traceIdBytes);

            if (ActivityContext.TryParse(traceId, null, out var parsedContext))
            {
                parentContext = parsedContext;
            }
        }
        if (consumeResult.Message.Headers.TryGetLastBytes("tracestate", out var traceStateBytes))
        {
            traceState = Encoding.UTF8.GetString(traceStateBytes);
        }

        _logger.LogDebug("RAW MESSAGE - Topic: {Topic}, Key: {Key}, Value: {Value}", 
            consumeResult.Topic, consumeResult.Message.Key, consumeResult.Message.Value);
        using var activity = ActivitySource.StartActivity(
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
            activity.SetTag("event.id", eventId);

            if (traceId != null)
            {
                activity.SetTag("trace.parent", traceId);
            }

            if (traceState != null)
            {
                activity.SetTag("trace.state", traceState);
            }
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
        {
            throw new InvalidOperationException($"Unknown event type: {eventTypeName}");
        }

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
        {
            _logger.LogDebug("Message {EventId} already in inbox, skipping", eventId);
        }
    }

    private bool IsTransientError(Exception ex)
    {
        return ex switch
        {
            TimeoutException => true,
            NpgsqlException { IsTransient: true } => true,
            DbUpdateException dbEx when dbEx.InnerException?.Message.Contains("deadlock") == true => true,
            HttpRequestException { StatusCode: not null } httpEx when (int)httpEx.StatusCode.Value >= 500 => true,
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
                { "source", Encoding.UTF8.GetBytes(DlqConstants.KafkaConsumerSource) }
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
            "Message moved to DLQ. Original topic: {Topic}, Offset: {Offset}, Error: {Error}",
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