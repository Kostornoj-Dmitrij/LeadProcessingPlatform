using IntegrationEvents.Base;

namespace IntegrationEvents.ScoringEvents;

/// <summary>
/// Публикуется Scoring Service при ошибке скоринга.
/// </summary>
public class LeadScoringFailedIntegrationEvent : IntegrationEvent
{
    public Guid LeadId { get; set; }

    public string Reason { get; set; } = string.Empty;
}