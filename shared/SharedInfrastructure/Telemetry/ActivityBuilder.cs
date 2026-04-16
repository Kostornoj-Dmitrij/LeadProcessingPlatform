using System.Diagnostics;
using AvroSchemas.Messages.Base;

namespace SharedInfrastructure.Telemetry;

/// <summary>
/// Билдер для создания Activity с тегами телеметрии
/// </summary>
public class ActivityBuilder(Activity? activity) : IDisposable
{
    private readonly Activity? _activity = activity;
    private bool _disposed;

    public static ActivityBuilder ForCommand(string commandName)
    {
        var activity = TelemetryConstants.ActivitySource.StartActivity(
            $"{TelemetrySpanNames.CommandHandler} {commandName}");
        return new ActivityBuilder(activity);
    }

    public static ActivityBuilder ForEvent(IntegrationEventAvro @event, string? customName = null)
    {
        var eventName = customName ?? @event.GetType().Name;
        var leadIdProp = @event.GetType().GetProperty("LeadId");
        var leadId = leadIdProp?.GetValue(@event) as Guid?;

        return ForEvent(eventName, leadId);
    }

    private static ActivityBuilder ForEvent(string eventName, Guid? leadId = null)
    {
        var activity = TelemetryConstants.ActivitySource.StartActivity(
            $"{TelemetrySpanNames.EventHandler} {eventName}");

        var builder = new ActivityBuilder(activity)
            .WithTag(TelemetryAttributes.EventName, eventName);

        if (leadId.HasValue)
        {
            builder.WithTag(TelemetryAttributes.LeadId, leadId.Value);
        }

        return builder;
    }

    public static ActivityBuilder ForHttpClient(string operationName, Guid? leadId = null)
    {
        var activity = TelemetryConstants.ActivitySource.StartActivity(
            operationName,
            ActivityKind.Client);

        var builder = new ActivityBuilder(activity);
        
        if (leadId.HasValue)
        {
            builder.WithTag(TelemetryAttributes.LeadId, leadId.Value);
        }

        return builder;
    }

    public static ActivityBuilder RestoreAndCreateActivity(
        string operationName,
        string? traceParent,
        ActivityKind kind = ActivityKind.Internal)
    {
        var activity = TelemetryRestorer.RestoreAndStartActivity(
            TelemetryConstants.ActivitySource,
            operationName,
            traceParent,
            kind);
        return new ActivityBuilder(activity);
    }

    public static ActivityBuilder ForProducer(string operation, string eventName)
    {
        var activity = TelemetryConstants.ActivitySource.StartActivity(
            $"{operation} {eventName}", 
            ActivityKind.Producer);
        return new ActivityBuilder(activity);
    }

    public ActivityBuilder WithTag(string key, object? value)
    {
        if (value != null && _activity != null)
        {
            _activity.SetTag(key, value);
        }
        return this;
    }

    public ActivityBuilder WithTags(params (string key, object? value)[] tags)
    {
        if (_activity == null) return this;

        foreach (var (key, value) in tags)
        {
            if (value != null)
            {
                _activity.SetTag(key, value);
            }
        }
        return this;
    }

    public ActivityBuilder WithFailureTags(string? reason = null, int? retryCount = null, string? failureType = null)
    {
        return WithTag(TelemetryAttributes.FailureReason, reason)
            .WithTag(TelemetryAttributes.FailureRetryCount, retryCount)
            .WithTag(TelemetryAttributes.FailureType, failureType);
    }

    public ActivityBuilder WithDistributionTags(
        string? target = null, 
        string? reason = null, 
        int? httpStatusCode = null,
        long? distributedAt = null)
    {
        return WithTag(TelemetryAttributes.DistributionTarget, target)
            .WithTag(TelemetryAttributes.DistributionReason, reason)
            .WithTag(TelemetryAttributes.DistributionHttpStatusCode, httpStatusCode)
            .WithTag(TelemetryAttributes.DistributionDistributedAt, distributedAt);
    }

    public ActivityBuilder WithEnrichmentTags(
        string? industry = null,
        string? companySize = null,
        int? version = null)
    {
        return WithTag(TelemetryAttributes.LeadIndustry, industry)
            .WithTag(TelemetryAttributes.LeadCompanySize, companySize)
            .WithTag(TelemetryAttributes.LeadEnrichmentVersion, version);
    }

    public ActivityBuilder WithScoringTags(
        int? score = null,
        int? qualifiedThreshold = null,
        int? appliedRulesCount = null)
    {
        return WithTag(TelemetryAttributes.LeadScore, score)
            .WithTag(TelemetryAttributes.LeadQualifiedThreshold, qualifiedThreshold)
            .WithTag(TelemetryAttributes.LeadAppliedRulesCount, appliedRulesCount);
    }

    public ActivityBuilder WithEnrichmentProcessorTags(
        Guid requestId,
        string companyName,
        int attempt,
        int maxRetries)
    {
        return WithTag(TelemetryAttributes.EnrichmentRequestId, requestId)
            .WithTag(TelemetryAttributes.EnrichmentCompanyName, companyName)
            .WithTag(TelemetryAttributes.EnrichmentAttempt, attempt)
            .WithTag(TelemetryAttributes.EnrichmentMaxRetries, maxRetries);
    }

    public ActivityBuilder WithScoringProcessorTags(
        Guid requestId,
        string companyName,
        int attempt,
        int maxRetries,
        bool hasEnrichedData)
    {
        return WithTag(TelemetryAttributes.ScoringRequestId, requestId)
            .WithTag(TelemetryAttributes.ScoringCompanyName, companyName)
            .WithTag(TelemetryAttributes.ScoringAttempt, attempt)
            .WithTag(TelemetryAttributes.ScoringMaxRetries, maxRetries)
            .WithTag(TelemetryAttributes.ScoringHasEnrichedData, hasEnrichedData);
    }

    public ActivityBuilder WithDistributionProcessorTags(
        Guid requestId,
        string companyName,
        int attempt,
        int maxRetries)
    {
        return WithTag(TelemetryAttributes.DistributionRequestId, requestId)
            .WithTag(TelemetryAttributes.DistributionCompanyName, companyName)
            .WithTag(TelemetryAttributes.DistributionAttempt, attempt)
            .WithTag(TelemetryAttributes.DistributionMaxRetries, maxRetries);
    }

    public ActivityBuilder WithDistributionClientTags(
        string target,
        string companyName,
        int score)
    {
        return WithTag(TelemetryAttributes.DistributionTarget, target)
            .WithTag(TelemetryAttributes.DistributionCompanyName, companyName)
            .WithTag(TelemetryAttributes.DistributionScore, score);
    }

    public ActivityBuilder WithKafkaConsumerTags(
        string eventTypeName,
        string eventName,
        string eventId,
        string topic,
        int partition,
        long offset,
        string consumerGroup,
        string serviceName,
        string? leadId)
    {
        return WithTag(TelemetryAttributes.EventType, eventTypeName)
            .WithTag(TelemetryAttributes.EventName, eventName)
            .WithTag(TelemetryAttributes.EventId, eventId)
            .WithTag(TelemetryAttributes.KafkaTopic, topic)
            .WithTag(TelemetryAttributes.KafkaPartition, partition)
            .WithTag(TelemetryAttributes.KafkaOffset, offset)
            .WithTag(TelemetryAttributes.KafkaConsumerGroup, consumerGroup)
            .WithTag(TelemetryAttributes.ServiceName, serviceName)
            .WithTag(TelemetryAttributes.LeadId, leadId)
            .WithTag(TelemetryAttributes.KafkaMessagingSystem, "kafka")
            .WithTag(TelemetryAttributes.KafkaMessagingDestination, topic)
            .WithTag(TelemetryAttributes.KafkaMessagingMessageId, eventId)
            .WithTag(TelemetryAttributes.KafkaMessagingOperation, "receive")
            .WithProcessingStep("kafka_consume");
    }

    public ActivityBuilder WithKafkaProducerTags(
        string eventType,
        string topic,
        string serviceName,
        string? leadId)
    {
        return WithTag(TelemetryAttributes.EventType, eventType)
            .WithTag(TelemetryAttributes.KafkaTopic, topic)
            .WithTag(TelemetryAttributes.ServiceName, serviceName)
            .WithTag(TelemetryAttributes.KafkaMessagingOperation, "publish")
            .WithTag(TelemetryAttributes.LeadId, leadId);
    }

    public void WithInboxProcessorTags(string eventType,
        string eventName,
        string leadId,
        string topic,
        Guid messageId,
        int processingAttempts)
    {
        WithTag(TelemetryAttributes.EventType, eventType)
            .WithTag(TelemetryAttributes.EventName, eventName)
            .WithTag(TelemetryAttributes.LeadId, leadId)
            .WithTag(TelemetryAttributes.KafkaTopic, topic)
            .WithTag(TelemetryAttributes.ProcessingStep, "inbox_process")
            .WithTag(TelemetryAttributes.InboxMessageId, messageId)
            .WithTag(TelemetryAttributes.InboxProcessingAttempts, processingAttempts);
    }

    public ActivityBuilder WithOutboxPublisherTags(
        string eventType,
        string eventName,
        string aggregateId,
        string aggregateType,
        Guid messageId,
        int processingAttempts)
    {
        return WithTag(TelemetryAttributes.EventType, eventType)
            .WithTag(TelemetryAttributes.EventName, eventName)
            .WithTag(TelemetryAttributes.LeadId, aggregateId)
            .WithTag(TelemetryAttributes.ProcessingStep, "outbox_publish")
            .WithTag(TelemetryAttributes.OutboxMessageId, messageId)
            .WithTag(TelemetryAttributes.OutboxAggregateType, aggregateType)
            .WithTag(TelemetryAttributes.OutboxProcessingAttempts, processingAttempts);
    }

    public ActivityBuilder WithProcessingStep(string step)
    {
        return WithTag(TelemetryAttributes.ProcessingStep, step);
    }

    public string? TraceId => _activity?.TraceId.ToString();

    public void SetTag(string key, object? value) => _activity?.SetTag(key, value);

    public void SetBaggage(string key, string? value) => _activity?.SetBaggage(key, value);

    public static implicit operator Activity?(ActivityBuilder builder) => builder._activity;

    public void Dispose()
    {
        if (!_disposed)
        {
            _activity?.Dispose();
            _disposed = true;
        }
    }
}