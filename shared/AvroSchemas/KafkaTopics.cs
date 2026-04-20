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
    public const string LeadEventsBase = "lead-events";
    public const string EnrichmentEventsBase = "enrichment-events";
    public const string ScoringEventsBase = "scoring-events";
    public const string DistributionEventsBase = "distribution-events";
    public const string NotificationEventsBase = "notification-events";
    public const string SagaEventsBase = "saga-events";

    private static readonly Dictionary<Type, string> TopicMappings = new();

    static KafkaTopics()
    {
        TopicMappings[typeof(LeadCreated)] = LeadEventsBase;
        TopicMappings[typeof(LeadQualified)] = LeadEventsBase;
        TopicMappings[typeof(LeadRejected)] = LeadEventsBase;
        TopicMappings[typeof(LeadDistributed)] = LeadEventsBase;
        TopicMappings[typeof(LeadDistributionFailed)] = LeadEventsBase;
        TopicMappings[typeof(LeadRejectedFinal)] = LeadEventsBase;
        TopicMappings[typeof(LeadDistributionFailedFinal)] = LeadEventsBase;
        TopicMappings[typeof(LeadDistributedFinal)] = LeadEventsBase;

        TopicMappings[typeof(LeadEnriched)] = EnrichmentEventsBase;
        TopicMappings[typeof(LeadEnrichmentFailed)] = EnrichmentEventsBase;
        TopicMappings[typeof(LeadEnrichmentCompensated)] = SagaEventsBase;

        TopicMappings[typeof(LeadScored)] = ScoringEventsBase;
        TopicMappings[typeof(LeadScoringFailed)] = ScoringEventsBase;
        TopicMappings[typeof(LeadScoringCompensated)] = SagaEventsBase;

        TopicMappings[typeof(DistributionSucceeded)] = DistributionEventsBase;
        TopicMappings[typeof(DistributionFailed)] = DistributionEventsBase;

        TopicMappings[typeof(NotificationSent)] = NotificationEventsBase;
    }

    public static string GetBaseTopic<TEvent>() where TEvent : IIntegrationEvent
        => GetBaseTopic(typeof(TEvent));

    public static string GetBaseTopic(Type eventType)
    {
        if (TopicMappings.TryGetValue(eventType, out var topic))
            return topic;
        throw new KeyNotFoundException($"No topic configured for event type {eventType.Name}");
    }
}