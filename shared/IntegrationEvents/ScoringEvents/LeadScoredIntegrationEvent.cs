using IntegrationEvents.Base;

namespace IntegrationEvents.ScoringEvents;

/// <summary>
/// Публикуется Scoring Service после успешного скоринга лида.
/// </summary>
public class LeadScoredIntegrationEvent : IntegrationEvent
{
    public Guid LeadId { get; set; }

    public int TotalScore { get; set; }

    public int QualifiedThreshold { get; set; }

    public List<string>? AppliedRules { get; set; }
}