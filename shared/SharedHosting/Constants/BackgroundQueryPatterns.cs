namespace SharedHosting.Constants;

/// <summary>
/// Паттерны для фильтрации фоновых запросов в телеметрии
/// </summary>
public static class BackgroundQueryPatterns
{
    public const string InboxMessages = "inbox_messages";
    public const string OutboxMessages = "outbox_messages";
    public const string PendingEnrichedData = "pending_enriched_data";
    public const string ScoringRequests = "scoring_requests";
    public const string ScoringRules = "scoring_rules";
    public const string EnrichmentRequests = "enrichment_requests";
    public const string DistributionRequests = "distribution_requests";
    
    public static readonly string[] All = 
    [
        InboxMessages,
        OutboxMessages,
        PendingEnrichedData,
        ScoringRequests,
        ScoringRules,
        EnrichmentRequests,
        DistributionRequests
    ];
}