using System.Diagnostics.Metrics;

namespace LeadService.Application.Metrics;

/// <summary>
/// Метрики для Lead Service
/// </summary>
public static class LeadMetrics
{
    private static readonly Meter Meter = new("LeadService.Metrics", "1.0.0");

    public static readonly Counter<int> LeadsCreated = 
        Meter.CreateCounter<int>("leads.created.total", description: "Total number of leads created");

    public static readonly Counter<int> LeadsQualified = 
        Meter.CreateCounter<int>("leads.qualified.total", description: "Total number of leads qualified");

    public static readonly Counter<int> LeadsRejected = 
        Meter.CreateCounter<int>("leads.rejected.total", description: "Total number of leads rejected");

    public static readonly Counter<int> LeadsDistributed = 
        Meter.CreateCounter<int>("leads.distributed.total", description: "Total number of leads distributed");

    public static readonly Counter<int> LeadsDistributionFailed = 
        Meter.CreateCounter<int>("leads.distribution_failed.total", description: "Total number of distribution failures");

    public static readonly Counter<int> LeadsClosed = 
        Meter.CreateCounter<int>("leads.closed.total", description: "Total number of leads closed");

    public static readonly Histogram<double> LeadProcessingDuration = 
        Meter.CreateHistogram<double>("lead.processing.duration", unit: "ms", description: "Time from lead creation to finalization");

    public static readonly Histogram<double> CommandHandlingDuration = 
        Meter.CreateHistogram<double>("lead.command.handling.duration", unit: "ms", description: "Command handling duration");
    
    public static readonly Histogram<double> EventHandlingDuration = 
        Meter.CreateHistogram<double>("lead.event.handling.duration", unit: "ms", description: "Event handling duration");
}