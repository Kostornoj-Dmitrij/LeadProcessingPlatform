namespace DistributionService.Infrastructure.Options;

/// <summary>
/// Настройки Distribution Service
/// </summary>
public class DistributionOptions
{
    public string TargetApiUrl { get; set; } = string.Empty;

    public int MaxRetryAttempts { get; set; } = 3;

    public int RetryDelaySeconds { get; set; } = 2;
}