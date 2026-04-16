using Confluent.SchemaRegistry;
using Microsoft.Extensions.Logging;
using AvroSchemas.Messages.DistributionEvents;
using AvroSchemas.Messages.EnrichmentEvents;
using AvroSchemas.Messages.LeadEvents;
using AvroSchemas.Messages.NotificationEvents;
using AvroSchemas.Messages.ScoringEvents;
using AvroSchema = Avro.Schema;

namespace AvroSchemas;

/// <summary>
/// Вспомогательный класс для регистрации Avro-схем в Schema Registry
/// </summary>
public static class SchemaRegistryHelper
{
    public static async Task RegisterAllSchemasAsync(
        ISchemaRegistryClient schemaRegistry,
        ILogger? logger = null)
    {
        var subjectsAndTypes = new Dictionary<string, Type>
        {
            [$"{KafkaTopics.LeadEvents}-{nameof(LeadCreated)}-value"] = typeof(LeadCreated),
            [$"{KafkaTopics.LeadEvents}-{nameof(LeadQualified)}-value"] = typeof(LeadQualified),
            [$"{KafkaTopics.LeadEvents}-{nameof(LeadRejected)}-value"] = typeof(LeadRejected),
            [$"{KafkaTopics.LeadEvents}-{nameof(LeadDistributed)}-value"] = typeof(LeadDistributed),
            [$"{KafkaTopics.LeadEvents}-{nameof(LeadDistributionFailed)}-value"] = typeof(LeadDistributionFailed),
            [$"{KafkaTopics.LeadEvents}-{nameof(LeadRejectedFinal)}-value"] = typeof(LeadRejectedFinal),
            [$"{KafkaTopics.LeadEvents}-{nameof(LeadDistributionFailedFinal)}-value"] = typeof(LeadDistributionFailedFinal),
            [$"{KafkaTopics.LeadEvents}-{nameof(LeadDistributedFinal)}-value"] = typeof(LeadDistributedFinal),

            [$"{KafkaTopics.EnrichmentEvents}-{nameof(LeadEnriched)}-value"] = typeof(LeadEnriched),
            [$"{KafkaTopics.EnrichmentEvents}-{nameof(LeadEnrichmentFailed)}-value"] = typeof(LeadEnrichmentFailed),

            [$"{KafkaTopics.ScoringEvents}-{nameof(LeadScored)}-value"] = typeof(LeadScored),
            [$"{KafkaTopics.ScoringEvents}-{nameof(LeadScoringFailed)}-value"] = typeof(LeadScoringFailed),

            [$"{KafkaTopics.DistributionEvents}-{nameof(DistributionSucceeded)}-value"] = typeof(DistributionSucceeded),
            [$"{KafkaTopics.DistributionEvents}-{nameof(DistributionFailed)}-value"] = typeof(DistributionFailed),

            [$"{KafkaTopics.SagaEvents}-{nameof(LeadEnrichmentCompensated)}-value"] = typeof(LeadEnrichmentCompensated),
            [$"{KafkaTopics.SagaEvents}-{nameof(LeadScoringCompensated)}-value"] = typeof(LeadScoringCompensated),

            [$"{KafkaTopics.NotificationEvents}-{nameof(NotificationSent)}-value"] = typeof(NotificationSent),

            [$"{KafkaTopics.LeadEvents}-{nameof(EnrichedData)}-value"] = typeof(EnrichedData)
        };

        foreach (var (subject, type) in subjectsAndTypes)
        {
            try
            {
                var avroSchema = GetAvroSchemaFromType(type);
                var schemaString = avroSchema.ToString();
                var confluentSchema = new Schema(schemaString, SchemaType.Avro);
                var schemaId = await schemaRegistry.RegisterSchemaAsync(subject, confluentSchema);

                logger?.LogInformation(
                    "Registered schema for subject '{Subject}', type '{Type}', schema ID: {SchemaId}",
                    subject, type.Name, schemaId);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, 
                    "Failed to register schema for subject '{Subject}', type '{Type}'",
                    subject, type.Name);
            }
        }
    }

    private static AvroSchema GetAvroSchemaFromType(Type type)
    {
        var instance = Activator.CreateInstance(type);
        var instanceSchemaProperty = type.GetProperty("Schema", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        if (instanceSchemaProperty?.GetValue(instance) is AvroSchema instanceSchema)
        {
            return instanceSchema;
        }

        throw new InvalidOperationException($"Cannot get Avro schema from type {type.Name}");
    }
}