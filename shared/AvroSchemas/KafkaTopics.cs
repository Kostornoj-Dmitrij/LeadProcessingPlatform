using AvroSchemas.Messages.DistributionEvents;
using AvroSchemas.Messages.EnrichmentEvents;
using AvroSchemas.Messages.LeadEvents;
using AvroSchemas.Messages.NotificationEvents;
using AvroSchemas.Messages.ScoringEvents;

namespace AvroSchemas;

/// <summary>
/// Конфигурация топиков Kafka и маппинга типов событий на топики
/// </summary>
public static class KafkaTopics
{
    public const string LeadEvents = "lead-events";
    public const string EnrichmentEvents = "enrichment-events";
    public const string ScoringEvents = "scoring-events";
    public const string DistributionEvents = "distribution-events";
    public const string NotificationEvents = "notification-events";
    public const string SagaEvents = "saga-events";

    private static readonly Dictionary<Type, string> TopicMappings = new();

    static KafkaTopics()
    {
        TopicMappings[typeof(LeadCreated)] = LeadEvents;
        TopicMappings[typeof(LeadQualified)] = LeadEvents;
        TopicMappings[typeof(LeadRejected)] = LeadEvents;
        TopicMappings[typeof(LeadDistributed)] = LeadEvents;
        TopicMappings[typeof(LeadDistributionFailed)] = LeadEvents;
        TopicMappings[typeof(LeadRejectedFinal)] = LeadEvents;
        TopicMappings[typeof(LeadDistributionFailedFinal)] = LeadEvents;
        TopicMappings[typeof(LeadDistributedFinal)] = LeadEvents;

        TopicMappings[typeof(LeadEnriched)] = EnrichmentEvents;
        TopicMappings[typeof(LeadEnrichmentFailed)] = EnrichmentEvents;
        TopicMappings[typeof(LeadEnrichmentCompensated)] = SagaEvents;

        TopicMappings[typeof(LeadScored)] = ScoringEvents;
        TopicMappings[typeof(LeadScoringFailed)] = ScoringEvents;
        TopicMappings[typeof(LeadScoringCompensated)] = SagaEvents;

        TopicMappings[typeof(DistributionSucceeded)] = DistributionEvents;
        TopicMappings[typeof(DistributionFailed)] = DistributionEvents;

        TopicMappings[typeof(NotificationSent)] = NotificationEvents;
    }

    public static string GetTopic<TEvent>() where TEvent : IIntegrationEvent
    {
        if (TopicMappings.TryGetValue(typeof(TEvent), out var topic))
            return topic;

        throw new KeyNotFoundException($"No topic configured for event type {typeof(TEvent).Name}");
    }

    public static string GetTopic(Type eventType)
    {
        if (TopicMappings.TryGetValue(eventType, out var topic))
            return topic;

        throw new KeyNotFoundException($"No topic configured for event type {eventType.Name}");
    }
}