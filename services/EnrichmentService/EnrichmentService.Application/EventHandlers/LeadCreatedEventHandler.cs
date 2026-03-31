using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using AvroSchemas.Messages.LeadEvents;
using EnrichmentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
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
        logger.LogInformation("Processing LeadCreated for lead {LeadId}", @event.LeadId);

        var existingRequest = await unitOfWork.Set<EnrichmentRequest>()
            .FirstOrDefaultAsync(x => x.LeadId == @event.LeadId, cancellationToken);
        if (existingRequest != null)
        {
            logger.LogInformation("Lead {LeadId} already has an enrichment request in status {Status}, skipping", 
                @event.LeadId, existingRequest.Status);
            return;
        }

        var currentActivity = Activity.Current;
        var traceParent = currentActivity != null 
            ? $"00-{currentActivity.TraceId}-{currentActivity.SpanId}-01"
            : null;
        var request = EnrichmentRequest.Create(
            leadId: @event.LeadId,
            companyName: @event.CompanyName,
            email: @event.Email,
            contactPerson: @event.ContactPerson,
            customFields: @event.CustomFields,
            traceParent: traceParent);

        await unitOfWork.Set<EnrichmentRequest>().AddAsync(request, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Enrichment request created for lead {LeadId}", @event.LeadId);
    }
}