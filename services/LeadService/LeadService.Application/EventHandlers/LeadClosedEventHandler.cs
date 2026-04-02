using System.Diagnostics;
using AvroSchemas.Messages.LeadEvents;
using LeadService.Application.Metrics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LeadService.Application.EventHandlers;

/// <summary>
/// Обработчик финальных событий лида для сбора метрик закрытия
/// </summary>
public class LeadClosedEventHandler(ILogger<LeadClosedEventHandler> logger) :
    INotificationHandler<LeadRejectedFinal>,
    INotificationHandler<LeadDistributionFailedFinal>,
    INotificationHandler<LeadDistributedFinal>
{
    public Task Handle(LeadRejectedFinal notification, CancellationToken cancellationToken)
    {
        LeadMetrics.LeadsClosed.Add(1, new TagList { { "final_status", "Rejected" } });
        logger.LogDebug("Lead {LeadId} closed with status Rejected", notification.LeadId);
        return Task.CompletedTask;
    }

    public Task Handle(LeadDistributionFailedFinal notification, CancellationToken cancellationToken)
    {
        LeadMetrics.LeadsClosed.Add(1, new TagList { { "final_status", "FailedDistribution" } });
        logger.LogDebug("Lead {LeadId} closed with status FailedDistribution", notification.LeadId);
        return Task.CompletedTask;
    }

    public Task Handle(LeadDistributedFinal notification, CancellationToken cancellationToken)
    {
        LeadMetrics.LeadsClosed.Add(1, new TagList { { "final_status", "Distributed" } });
        logger.LogDebug("Lead {LeadId} closed with status Distributed", notification.LeadId);
        return Task.CompletedTask;
    }
}