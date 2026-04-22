using System.Text;
using AvroSchemas.Naming;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SharedHosting.Constants;
using SharedHosting.Extensions;
using SharedHosting.Options;
using SharedInfrastructure.Constants;

namespace SharedInfrastructure.Inbox;

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
        ILogger<KafkaDeadLetterQueue> logger,
        INamingConvention naming,
        string serviceName)
    {
        _logger = logger;

        var kafkaOptions = configuration.GetSection(KafkaOptions.SectionName).Get<KafkaOptions>();

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = configuration[ConfigurationKeys.KafkaBootstrapServers],
            EnableIdempotence = true,
            Acks = Acks.All
        };
        producerConfig.ApplySaslConfig(kafkaOptions);

        _producer = new ProducerBuilder<string, string>(producerConfig).Build();
        _dlqTopic = naming.GetDlqTopicName(serviceName);
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
                { KafkaHeaderKeys.OriginalTopic, Encoding.UTF8.GetBytes(originalTopic) },
                { KafkaHeaderKeys.ErrorMessage, Encoding.UTF8.GetBytes(exception.Message) },
                { KafkaHeaderKeys.ErrorType, Encoding.UTF8.GetBytes(exception.GetType().Name) },
                { KafkaHeaderKeys.Timestamp, Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")) },
                { KafkaHeaderKeys.Source, KafkaHeaderValues.SourceInboxProcessorBytes }
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