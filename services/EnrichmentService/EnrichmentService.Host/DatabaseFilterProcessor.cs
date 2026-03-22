using System.Diagnostics;

namespace EnrichmentService.Host;

/// <summary>
/// Процессор OpenTelemetry для фильтрации телеметрии от фоновых процессов
/// </summary>
public class DatabaseFilterProcessor : OpenTelemetry.BaseProcessor<Activity>
{
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
            if (tag.Key == "db.statement")
            {
                var sql = tag.Value ?? "";
                if (sql.Contains("inbox_messages") || sql.Contains("outbox_messages"))
                {
                    return true;
                }
            }
        }

        if (activity.DisplayName.Contains("inbox") ||
            activity.DisplayName.Contains("outbox"))
        {
            return true;
        }

        return false;
    }
}