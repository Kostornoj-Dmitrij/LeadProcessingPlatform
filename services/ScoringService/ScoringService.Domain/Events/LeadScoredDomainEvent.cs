using AvroSchemas;
using AvroSchemas.Messages.ScoringEvents;
using SharedKernel.Events;

namespace ScoringService.Domain.Events;

/// <summary>
/// Доменное событие - лид успешно прошел скоринг
/// </summary>
public class LeadScoredDomainEvent(
    Guid leadId,
    int totalScore,
    int qualifiedThreshold,
    List<string> appliedRules) : DomainEvent
{
    public Guid LeadId { get; } = leadId;
    public int TotalScore { get; } = totalScore;
    public int QualifiedThreshold { get; } = qualifiedThreshold;
    public List<string> AppliedRules { get; } = appliedRules;

    public override IIntegrationEvent ToIntegrationEvent()
    {
        return new LeadScored
        {
            EventId = EventId,
            OccurredOnUtc = new DateTimeOffset(OccurredOn).ToUnixTimeMilliseconds(),
            EventType = GetType().Name,
            SchemaVersion = 1,
            LeadId = LeadId,
            TotalScore = TotalScore,
            QualifiedThreshold = QualifiedThreshold,
            AppliedRules = AppliedRules
        };
    }
}