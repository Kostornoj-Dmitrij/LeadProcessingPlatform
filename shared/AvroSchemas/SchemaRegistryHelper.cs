using Confluent.SchemaRegistry;
using Microsoft.Extensions.Logging;
using AvroSchemas.Messages.DistributionEvents;
using AvroSchemas.Messages.EnrichmentEvents;
using AvroSchemas.Messages.LeadEvents;
using AvroSchemas.Messages.NotificationEvents;
using AvroSchemas.Messages.ScoringEvents;
using AvroSchemas.Naming;
using AvroSchema = Avro.Schema;

namespace AvroSchemas;

/// <summary>
/// Вспомогательный класс для регистрации Avro-схем в Schema Registry
/// </summary>
public static class SchemaRegistryHelper
{
    public static async Task RegisterAllSchemasAsync(
        ISchemaRegistryClient schemaRegistry,
        INamingConvention naming,
        ILogger? logger = null)
    {
        var leadEventsTopic = naming.GetTopicName(KafkaTopics.LeadEventsBase);
        var enrichmentEventsTopic = naming.GetTopicName(KafkaTopics.EnrichmentEventsBase);
        var scoringEventsTopic = naming.GetTopicName(KafkaTopics.ScoringEventsBase);
        var distributionEventsTopic = naming.GetTopicName(KafkaTopics.DistributionEventsBase);
        var sagaEventsTopic = naming.GetTopicName(KafkaTopics.SagaEventsBase);
        var notificationEventsTopic = naming.GetTopicName(KafkaTopics.NotificationEventsBase);

        var subjectsAndTypes = new Dictionary<string, Type>
        {
            [$"{leadEventsTopic}-{nameof(LeadCreated)}-value"] = typeof(LeadCreated),
            [$"{leadEventsTopic}-{nameof(LeadQualified)}-value"] = typeof(LeadQualified),
            [$"{leadEventsTopic}-{nameof(LeadRejected)}-value"] = typeof(LeadRejected),
            [$"{leadEventsTopic}-{nameof(LeadDistributed)}-value"] = typeof(LeadDistributed),
            [$"{leadEventsTopic}-{nameof(LeadDistributionFailed)}-value"] = typeof(LeadDistributionFailed),
            [$"{leadEventsTopic}-{nameof(LeadRejectedFinal)}-value"] = typeof(LeadRejectedFinal),
            [$"{leadEventsTopic}-{nameof(LeadDistributionFailedFinal)}-value"] = typeof(LeadDistributionFailedFinal),
            [$"{leadEventsTopic}-{nameof(LeadDistributedFinal)}-value"] = typeof(LeadDistributedFinal),

            [$"{enrichmentEventsTopic}-{nameof(LeadEnriched)}-value"] = typeof(LeadEnriched),
            [$"{enrichmentEventsTopic}-{nameof(LeadEnrichmentFailed)}-value"] = typeof(LeadEnrichmentFailed),

            [$"{scoringEventsTopic}-{nameof(LeadScored)}-value"] = typeof(LeadScored),
            [$"{scoringEventsTopic}-{nameof(LeadScoringFailed)}-value"] = typeof(LeadScoringFailed),

            [$"{distributionEventsTopic}-{nameof(DistributionSucceeded)}-value"] = typeof(DistributionSucceeded),
            [$"{distributionEventsTopic}-{nameof(DistributionFailed)}-value"] = typeof(DistributionFailed),

            [$"{sagaEventsTopic}-{nameof(LeadEnrichmentCompensated)}-value"] = typeof(LeadEnrichmentCompensated),
            [$"{sagaEventsTopic}-{nameof(LeadScoringCompensated)}-value"] = typeof(LeadScoringCompensated),

            [$"{notificationEventsTopic}-{nameof(NotificationSent)}-value"] = typeof(NotificationSent),

            [$"{leadEventsTopic}-{nameof(EnrichedData)}-value"] = typeof(EnrichedData)
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