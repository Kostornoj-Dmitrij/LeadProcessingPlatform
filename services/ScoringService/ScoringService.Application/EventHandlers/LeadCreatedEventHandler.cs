using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AvroSchemas.Messages.LeadEvents;
using ScoringService.Domain.Entities;
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
        logger.LogInformation("Processing LeadCreated for lead {LeadId}", @event.LeadId);

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

        var request = ScoringRequest.Create(
            leadId: @event.LeadId,
            companyName: @event.CompanyName,
            email: @event.Email,
            contactPerson: @event.ContactPerson,
            customFields: @event.CustomFields,
            enrichedData: enrichedDataJson);

        await unitOfWork.Set<ScoringRequest>().AddAsync(request, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Scoring request created for lead {LeadId} (has enriched data: {HasEnrichedData})", 
            @event.LeadId, enrichedDataJson != null);
    }
}