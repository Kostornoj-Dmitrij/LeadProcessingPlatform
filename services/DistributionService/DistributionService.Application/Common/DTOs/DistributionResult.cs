namespace DistributionService.Application.Common.DTOs;

/// <summary>
/// Результат отправки лида в целевую систему
/// </summary>
public record DistributionResult(bool IsSuccess, string? ResponseData, string? ErrorMessage);