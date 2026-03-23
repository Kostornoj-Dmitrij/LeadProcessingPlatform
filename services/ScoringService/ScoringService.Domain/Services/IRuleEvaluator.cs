using ScoringService.Domain.Entities;
using IntegrationEvents.LeadEvents;

namespace ScoringService.Domain.Services;

/// <summary>
/// Интерфейс для оценки правил скоринга
/// </summary>
public interface IRuleEvaluator
{
    Task<bool> EvaluateAsync(
        ScoringRule rule, 
        ScoringRequest request, 
        EnrichedDataDto? enrichedData,
        CancellationToken cancellationToken = default);
}