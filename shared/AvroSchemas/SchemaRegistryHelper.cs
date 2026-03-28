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
            ["lead-events-LeadCreated-value"] = typeof(LeadCreated),
            ["lead-events-LeadQualified-value"] = typeof(LeadQualified),
            ["lead-events-LeadRejected-value"] = typeof(LeadRejected),
            ["lead-events-LeadDistributed-value"] = typeof(LeadDistributed),
            ["lead-events-LeadDistributionFailed-value"] = typeof(LeadDistributionFailed),
            ["lead-events-LeadRejectedFinal-value"] = typeof(LeadRejectedFinal),
            ["lead-events-LeadDistributionFailedFinal-value"] = typeof(LeadDistributionFailedFinal),
            ["lead-events-LeadDistributedFinal-value"] = typeof(LeadDistributedFinal),

            ["enrichment-events-LeadEnriched-value"] = typeof(LeadEnriched),
            ["enrichment-events-LeadEnrichmentFailed-value"] = typeof(LeadEnrichmentFailed),

            ["scoring-events-LeadScored-value"] = typeof(LeadScored),
            ["scoring-events-LeadScoringFailed-value"] = typeof(LeadScoringFailed),

            ["distribution-events-DistributionSucceeded-value"] = typeof(DistributionSucceeded),
            ["distribution-events-DistributionFailed-value"] = typeof(DistributionFailed),

            ["saga-events-LeadEnrichmentCompensated-value"] = typeof(LeadEnrichmentCompensated),
            ["saga-events-LeadScoringCompensated-value"] = typeof(LeadScoringCompensated),

            ["notification-events-NotificationSent-value"] = typeof(NotificationSent),

            ["lead-events-EnrichedData-value"] = typeof(EnrichedData)
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