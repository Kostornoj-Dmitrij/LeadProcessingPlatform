using IntegrationEvents.Base;

namespace IntegrationEvents.ScoringEvents;

/// <summary>
/// Публикуется Scoring Service после успешной компенсации (отката) скоринга.
/// </summary>
public class LeadScoringCompensatedIntegrationEvent : IntegrationEvent
{
    public Guid LeadId { get; set; }

    public bool Compensated { get; set; } = true;
}