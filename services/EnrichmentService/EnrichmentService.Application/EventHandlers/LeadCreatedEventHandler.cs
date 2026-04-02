using System.Diagnostics;
using AvroSchemas.Messages.LeadEvents;
using EnrichmentService.Application.Metrics;
using EnrichmentService.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedInfrastructure.Telemetry;
using SharedKernel.Base;

namespace EnrichmentService.Application.EventHandlers;

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
                (TelemetryAttributes.ProcessingStep, "enrichment_request_creation"));
        logger.LogInformation("Processing LeadCreated for lead {LeadId}", @event.LeadId);

        var existingRequest = await unitOfWork.Set<EnrichmentRequest>()
            .FirstOrDefaultAsync(x => x.LeadId == @event.LeadId, cancellationToken);

        if (existingRequest != null)
        {
            logger.LogInformation("Lead {LeadId} already has an enrichment request in status {Status}, skipping",
                @event.LeadId, existingRequest.Status);
            return;
        }

        var traceParent = TelemetryContext.GetTraceParent();

        var request = EnrichmentRequest.Create(
            leadId: @event.LeadId,
            companyName: @event.CompanyName,
            email: @event.Email,
            contactPerson: @event.ContactPerson,
            customFields: @event.CustomFields,
            traceParent: traceParent);

        await unitOfWork.Set<EnrichmentRequest>().AddAsync(request, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        EnrichmentMetrics.EnrichmentRequests.Add(1, new TagList { { "status", "pending" } });
        logger.LogInformation("Enrichment request created for lead {LeadId}", @event.LeadId);
    }
}