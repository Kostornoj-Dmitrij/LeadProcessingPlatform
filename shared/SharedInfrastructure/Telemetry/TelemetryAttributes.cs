namespace SharedInfrastructure.Telemetry;

/// <summary>
/// Ключи атрибутов для тегирования спанов
/// </summary>
public static class TelemetryAttributes
{
    public const string LeadId = "lead.id";
    public const string LeadStatus = "lead.status";
    public const string LeadScore = "lead.score";
    public const string LeadSource = "lead.source";
    public const string LeadCompany = "lead.company";
    public const string LeadEmail = "lead.email";

    public const string EventType = "event.type";
    public const string EventId = "event.id";
    public const string EventName = "event.name";

    public const string KafkaTopic = "kafka.topic";
    public const string KafkaPartition = "kafka.partition";
    public const string KafkaOffset = "kafka.offset";
    public const string KafkaConsumerGroup = "kafka.consumer.group";

    public const string ProcessingStep = "processing.step";

    public const string ServiceName = "service.name";
    public const string DistributionTarget = "distribution.target";
    public const string DistributionCompanyName = "distribution.company_name";
    public const string DistributionScore = "distribution.score";
    public const string DistributionReason = "distribution.reason";
    public const string DistributionHttpStatusCode = "distribution.http_status_code";
    public const string DistributionSuccess = "distribution.success";
    public const string DistributionMode = "distribution.mode";
    public const string DistributionForcedFailure = "distribution.forced_failure";
    public const string DistributionDistributedAt = "distribution.distributed_at";
    public const string DistributionRequestId = "distribution.request_id";
    public const string DistributionAttempt = "distribution.attempt";
    public const string DistributionMaxRetries = "distribution.max_retries";

    public const string EnrichmentRequestId = "enrichment.request_id";
    public const string EnrichmentCompanyName = "enrichment.company_name";
    public const string EnrichmentAttempt = "enrichment.attempt";
    public const string EnrichmentMaxRetries = "enrichment.max_retries";
    public const string EnrichmentIndustry = "enrichment.industry";
    public const string EnrichmentCompanySize = "enrichment.company_size";

    public const string ScoringRequestId = "scoring.request_id";
    public const string ScoringCompanyName = "scoring.company_name";
    public const string ScoringAttempt = "scoring.attempt";
    public const string ScoringMaxRetries = "scoring.max_retries";
    public const string ScoringHasEnrichedData = "scoring.has_enriched_data";

    public const string InboxMessageId = "inbox.message_id";
    public const string InboxProcessingAttempts = "inbox.processing_attempts";

    public const string OutboxMessageId = "outbox.message_id";
    public const string OutboxAggregateType = "outbox.aggregate_type";
    public const string OutboxProcessingAttempts = "outbox.processing_attempts";

    public const string KafkaMessagingSystem = "messaging.system";
    public const string KafkaMessagingDestination = "messaging.destination";
    public const string KafkaMessagingMessageId = "messaging.message_id";
    public const string KafkaMessagingOperation = "messaging.operation";

    public const string HttpStatusCode = "http.status_code";

    public const string LeadExternalId = "lead.external_id";
    public const string LeadHasCustomFields = "lead.has_custom_fields";
    public const string LeadIndustry = "lead.industry";
    public const string LeadCompanySize = "lead.company_size";
    public const string LeadEnrichmentVersion = "lead.enrichment_version";
    public const string LeadQualifiedThreshold = "lead.qualified_threshold";
    public const string LeadAppliedRulesCount = "lead.applied_rules_count";

    public const string FailureReason = "failure.reason";
    public const string FailureRetryCount = "failure.retry_count";
    public const string FailureType = "failure.type";

    public const string Error = "error";
    public const string ErrorType = "error.type";
}