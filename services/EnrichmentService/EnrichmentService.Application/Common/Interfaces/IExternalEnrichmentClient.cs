using EnrichmentService.Application.Common.DTOs;

namespace EnrichmentService.Application.Common.Interfaces;

/// <summary>
/// Клиент для взаимодействия с внешним API обогащения данных
/// </summary>
public interface IExternalEnrichmentClient
{
    Task<EnrichmentResponse> EnrichAsync(string companyName, Dictionary<string, string>? customFields, CancellationToken cancellationToken);
}