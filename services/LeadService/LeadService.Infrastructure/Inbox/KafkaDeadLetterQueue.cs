using System.Text;
using Confluent.Kafka;
using LeadService.Domain.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LeadService.Infrastructure.Inbox;

/// <summary>
/// Реализация Dead Letter Queue через Kafka
/// </summary>
public class KafkaDeadLetterQueue : IDeadLetterQueue
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaDeadLetterQueue> _logger;
    private readonly string _dlqTopic;

    public KafkaDeadLetterQueue(
        IConfiguration configuration,
        ILogger<KafkaDeadLetterQueue> logger)
    {
        _logger = logger;

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"],
            EnableIdempotence = true,
            Acks = Acks.All
        };

        _producer = new ProducerBuilder<string, string>(producerConfig).Build();
        _dlqTopic = configuration["Kafka:DlqTopic"] ?? "lead-service-dlq";
    }

    public async Task SendAsync(
        string originalTopic, 
        Message<string, string> message, 
        Exception exception,
        CancellationToken cancellationToken = default)
    {
        var deadLetterMessage = new Message<string, string>
        {
            Key = message.Key,
            Value = message.Value,
            Headers = new Headers
            {
                { "original-topic", Encoding.UTF8.GetBytes(originalTopic) },
                { "error-message", Encoding.UTF8.GetBytes(exception.Message) },
                { "error-type", Encoding.UTF8.GetBytes(exception.GetType().Name) },
                { "timestamp", Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")) },
                { "source", Encoding.UTF8.GetBytes(DlqConstants.InboxProcessorSource) }
            }
        };

        if (message.Headers != null)
        {
            foreach (var header in message.Headers)
            {
                deadLetterMessage.Headers.Add(header.Key, header.GetValueBytes());
            }
        }

        await _producer.ProduceAsync(_dlqTopic, deadLetterMessage, cancellationToken);

        _logger.LogWarning(
            "Message moved to DLQ. Original topic: {Topic}, Key: {Key}, Error: {Error}",
            originalTopic,
            message.Key,
            exception.Message);
    }

    public void Dispose()
    {
        _producer.Dispose();
    }
}