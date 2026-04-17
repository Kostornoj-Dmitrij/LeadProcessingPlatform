using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SharedHosting.Extensions;

/// <summary>
/// Расширения для настройки фильтрации логов через конфигурацию
/// </summary>
public static class LoggingExtensions
{
    public static IServiceCollection AddTelemetryLogFilters(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddLogging(builder =>
        {
            var categoryLevels = configuration.GetSection("Logging:CategoryLevels").Get<Dictionary<string, LogLevel>>();
            if (categoryLevels != null)
            {
                foreach (var kv in categoryLevels)
                {
                    builder.AddFilter(kv.Key, kv.Value);
                }
            }
        });

        return services;
    }
}