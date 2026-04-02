using System.Diagnostics.Metrics;

namespace NotificationService.Application.Metrics;

/// <summary>
/// Метрики для Notification Service
/// </summary>
public static class NotificationMetrics
{
    private static readonly Meter Meter = new("NotificationService.Metrics", "1.0.0");

    public static readonly Counter<int> NotificationsSent = 
        Meter.CreateCounter<int>("notifications.sent.total", 
            description: "Total number of notifications sent by type and channel");

    public static readonly Counter<int> NotificationsFailed = 
        Meter.CreateCounter<int>("notifications.failed.total", 
            description: "Total number of failed notifications by type, channel and reason");
}