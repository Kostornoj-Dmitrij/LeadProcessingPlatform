using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace SharedHosting.HealthChecks;

/// <summary>
/// Проверка статуса сервиса
/// </summary>
public class ServiceHealthCheck(ILogger<ServiceHealthCheck> logger) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Task.FromResult(HealthCheckResult.Healthy("Service is operational"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy("Service is unhealthy", ex));
        }
    }
}