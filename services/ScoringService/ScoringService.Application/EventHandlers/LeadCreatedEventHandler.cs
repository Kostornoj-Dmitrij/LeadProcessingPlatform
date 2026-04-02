using System.Diagnostics;
using AvroSchemas.Messages.LeadEvents;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScoringService.Application.Metrics;
using ScoringService.Domain.Entities;
using SharedInfrastructure.Telemetry;
using SharedKernel.Base;

namespace ScoringService.Application.EventHandlers;

/// <summary>
/// Обработчик события LeadCreated
/// </summary>
public class LeadCreatedEventHandler(
    IUnitOfWork unitOfWork,
    ILogger<LeadCreatedEventHandler> logger)
    : INotificationHandler<LeadCreated>
{
    public async Task Handle(LeadCreated @event, CancellationToken cancellationToken)
    {
        using var activity = TelemetryConstants.ActivitySource.StartEventHandlerSpan("LeadCreated")!
            .AddTags(
                (TelemetryAttributes.LeadId, @event.LeadId),
                (TelemetryAttributes.EventName, "LeadCreated"),
                (TelemetryAttributes.LeadSource, @event.Source),
                (TelemetryAttributes.LeadCompany, @event.CompanyName),
                (TelemetryAttributes.ProcessingStep, "scoring_request_creation"));

        var existingRequest = await unitOfWork.Set<ScoringRequest>()
            .FirstOrDefaultAsync(x => x.LeadId == @event.LeadId, cancellationToken);

        if (existingRequest != null)
        {
            logger.LogInformation("Lead {LeadId} already has a scoring request in status {Status}, skipping",
                @event.LeadId, existingRequest.Status);
            return;
        }

        string? enrichedDataJson = null;
        var pendingData = await unitOfWork.Set<PendingEnrichedData>()
            .FirstOrDefaultAsync(x => x.LeadId == @event.LeadId && !x.IsProcessed, cancellationToken);

        if (pendingData != null)
        {
            enrichedDataJson = pendingData.EnrichedDataJson;
            logger.LogInformation("Found pending enriched data for lead {LeadId}, will use for scoring", @event.LeadId);

            pendingData.MarkAsProcessed();
        }

        var traceParent = TelemetryContext.GetTraceParent();

        var request = ScoringRequest.Create(
            leadId: @event.LeadId,
            companyName: @event.CompanyName,
            email: @event.Email,
            contactPerson: @event.ContactPerson,
            customFields: @event.CustomFields,
            enrichedData: enrichedDataJson,
            traceParent: traceParent);

        await unitOfWork.Set<ScoringRequest>().AddAsync(request, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        ScoringMetrics.ScoringRequests.Add(1, new TagList { { "status", "pending" } });
        logger.LogInformation("Scoring request created for lead {LeadId} (has enriched data: {HasEnrichedData})", 
            @event.LeadId, enrichedDataJson != null);
    }
}