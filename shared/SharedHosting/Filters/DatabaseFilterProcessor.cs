using System.Diagnostics;
using OpenTelemetry;

namespace SharedHosting.Filters;

/// <summary>
/// Процессор OpenTelemetry для фильтрации телеметрии от фоновых процессов
/// </summary>
public class DatabaseFilterProcessor : BaseProcessor<Activity>
{
    private static readonly string[] BackgroundQueryPatterns = 
    [
        "inbox_messages", 
        "outbox_messages", 
        "pending_enriched_data",
        "scoring_requests",
        "enrichment_requests"
    ];

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