using DistributionService.Application.Common.DTOs;

namespace DistributionService.Application.Common.Interfaces;

/// <summary>
/// Интерфейс для отправки лидов в целевую систему
/// </summary>
public interface IDistributionTargetClient
{
    Task<DistributionResult> SendAsync(
        Guid leadId,
        string companyName,
        string email,
        int score,
        Dictionary<string, string>? customFields,
        string target,
        CancellationToken cancellationToken = default);
}