using System.Text.Json;
using EnrichmentService.Application.Common.DTOs;
using EnrichmentService.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace EnrichmentService.Infrastructure.Clients;

/// <summary>
/// Эмулятор клиента внешнего API обогащения данных
/// </summary>
public class ExternalEnrichmentClient(ILogger<ExternalEnrichmentClient> logger) : IExternalEnrichmentClient
{
    public async Task<EnrichmentResponse> EnrichAsync(string companyName, Dictionary<string, string>? customFields, CancellationToken cancellationToken)
    {
        logger.LogInformation("Enriching company: {CompanyName}", companyName);

        await Task.Delay(100, cancellationToken);

        if (customFields != null &&
            customFields.TryGetValue("industry", out var industry) &&
            industry == "Unknown")
        {
            logger.LogWarning("Enrichment failed for company {CompanyName}: Unknown industry", companyName);
            return EnrichmentResponse.Failure("External API timeout after 3 retries");
        }

        var enrichedData = new
        {
            Industry = customFields?.GetValueOrDefault("industry") ?? "Technology",
            CompanySize = "101-500",
            Website = $"https://{companyName.Replace(" ", "").ToLower()}.com",
            RevenueRange = "$10M-$50M",
            RawResponse = JsonSerializer.Serialize(new
            {
                success = true, data = new
                {
                    industry = "Technology", employees = "101-500"
                }
            })
        };

        return EnrichmentResponse.Success(
            enrichedData.Industry,
            enrichedData.CompanySize,
            enrichedData.Website,
            enrichedData.RevenueRange,
            enrichedData.RawResponse);
    }
}