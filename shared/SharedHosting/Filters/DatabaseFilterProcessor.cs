using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using OpenTelemetry;
using SharedHosting.Options;

namespace SharedHosting.Filters;

/// <summary>
/// Процессор OpenTelemetry для фильтрации телеметрии от фоновых процессов
/// </summary>
public class DatabaseFilterProcessor(IConfiguration configuration) : BaseProcessor<Activity>
{
    private const string DbStatementTag = "db.statement";
    private static readonly string[] BackgroundQueryPatterns = Constants.BackgroundQueryPatterns.All;
    private readonly OpenTelemetryOptions _otelOptions =
        configuration.GetSection(OpenTelemetryOptions.SectionName).Get<OpenTelemetryOptions>()
        ?? new OpenTelemetryOptions();

    public override void OnEnd(Activity activity)
    {
        if (_otelOptions.FilterBackgroundQueries && IsBackgroundDatabaseQuery(activity))
        {
            activity.IsAllDataRequested = false;
        }
    }

    private bool IsBackgroundDatabaseQuery(Activity activity)
    {
        foreach (var tag in activity.Tags)
        {
            if (tag.Key == DbStatementTag)
            {
                var sql = tag.Value ?? "";
                foreach (var pattern in BackgroundQueryPatterns)
                {
                    if (sql.Contains(pattern))
                        return true;
                }
            }
        }

        foreach (var pattern in BackgroundQueryPatterns)
        {
            if (activity.DisplayName.Contains(pattern))
                return true;
        }

        return false;
    }
}