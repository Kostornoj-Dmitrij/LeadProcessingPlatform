using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SharedHosting.Extensions;

/// <summary>
/// Расширения для работы с Kafka
/// </summary>
public static class KafkaExtensions
{
    public static async Task WaitForKafkaTopicsAsync(
        IServiceProvider services,
        string[] requiredTopics,
        int maxRetries = 60,
        TimeSpan? retryDelay = null)
    {
        var logger = services.GetRequiredService<ILogger<object>>();
        var configuration = services.GetRequiredService<IConfiguration>();

        var bootstrapServers = configuration["Kafka:BootstrapServers"];
        var delay = retryDelay ?? TimeSpan.FromSeconds(2);

        using var adminClient = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = bootstrapServers
        }).Build();

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
                var existingTopics = metadata.Topics
                    .Where(t => t.Error.Code == ErrorCode.NoError)
                    .Select(t => t.Topic)
                    .ToHashSet();

                var missingTopics = requiredTopics.Except(existingTopics).ToList();

                if (!missingTopics.Any())
                {
                    logger.LogInformation("All Kafka topics are available");
                    return;
                }

                logger.LogInformation("Waiting for topics: {MissingTopics} (attempt {Attempt}/{MaxRetries})",
                    string.Join(", ", missingTopics), attempt, maxRetries);
            }
            catch (Exception ex)
            {
                logger.LogWarning("Kafka not ready: {Message} (attempt {Attempt}/{MaxRetries})",
                    ex.Message, attempt, maxRetries);
            }

            await Task.Delay(delay);
        }

        logger.LogWarning("Not all topics are available after {MaxRetries} attempts. Continuing anyway...", maxRetries);
    }
}