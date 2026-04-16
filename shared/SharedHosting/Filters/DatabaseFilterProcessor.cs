using System.Diagnostics;
using OpenTelemetry;

namespace SharedHosting.Filters;

/// <summary>
/// Процессор OpenTelemetry для фильтрации телеметрии от фоновых процессов
/// </summary>
public class DatabaseFilterProcessor : BaseProcessor<Activity>
{
    private const string DbStatementTag = "db.statement";
    private static readonly string[] BackgroundQueryPatterns = Constants.BackgroundQueryPatterns.All;

    public override void OnEnd(Activity activity)
    {
        if (IsBackgroundDatabaseQuery(activity))
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