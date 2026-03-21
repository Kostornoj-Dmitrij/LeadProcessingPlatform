namespace EnrichmentService.Application.Common.DTOs;

/// <summary>
/// Результат запроса на обогащение данных
/// </summary>
public record EnrichmentResponse(bool IsSuccess, string? Industry, string? CompanySize, string? Website, string? RevenueRange, string? RawResponse, string? ErrorMessage = null)
{
    public static EnrichmentResponse Success(string industry, string companySize, string? website, string? revenueRange, string? rawResponse)
        => new(true, industry, companySize, website, revenueRange, rawResponse);

    public static EnrichmentResponse Failure(string errorMessage)
        => new(false, null, null, null, null, null, errorMessage);
}