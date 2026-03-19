namespace IntegrationEvents;

/// <summary>
/// Константы с названиями топиков Kafka для всех событий.
/// </summary>
public static class KafkaTopics
{
    private const string LeadEvents = "lead-events";
    private const string EnrichmentEvents = "enrichment-events";
    private const string ScoringEvents = "scoring-events";
    private const string DistributionEvents = "distribution-events";
    private const string NotificationEvents = "notification-events";
    private const string SagaEvents = "saga-events";
    private const string DeadLetterQueue = "lead-service-dlq";
    
    public static class Mappings
    {
        public static readonly Dictionary<Type, string> EventToTopic = new()
        {
            { typeof(LeadEvents.LeadCreatedIntegrationEvent), LeadEvents },
            { typeof(LeadEvents.LeadQualifiedIntegrationEvent), LeadEvents },
            { typeof(LeadEvents.LeadRejectedIntegrationEvent), LeadEvents },
            { typeof(LeadEvents.LeadDistributedIntegrationEvent), LeadEvents },
            { typeof(LeadEvents.LeadDistributionFailedIntegrationEvent), LeadEvents },
            { typeof(LeadEvents.LeadRejectedFinalIntegrationEvent), LeadEvents },
            { typeof(LeadEvents.LeadDistributionFailedFinalIntegrationEvent), LeadEvents },
            { typeof(LeadEvents.LeadDistributedFinalIntegrationEvent), LeadEvents },
            
            { typeof(EnrichmentEvents.LeadEnrichedIntegrationEvent), EnrichmentEvents },
            { typeof(EnrichmentEvents.LeadEnrichmentFailedIntegrationEvent), EnrichmentEvents },
            { typeof(EnrichmentEvents.LeadEnrichmentCompensatedIntegrationEvent), SagaEvents },
            
            { typeof(ScoringEvents.LeadScoredIntegrationEvent), ScoringEvents },
            { typeof(ScoringEvents.LeadScoringFailedIntegrationEvent), ScoringEvents },
            { typeof(ScoringEvents.LeadScoringCompensatedIntegrationEvent), SagaEvents },
            
            { typeof(DistributionEvents.DistributionSucceededIntegrationEvent), DistributionEvents },
            { typeof(DistributionEvents.DistributionFailedIntegrationEvent), DistributionEvents },
            
            { typeof(NotificationEvents.NotificationSentIntegrationEvent), NotificationEvents }
        };
    }
}