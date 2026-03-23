using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using IntegrationEvents.EnrichmentEvents;
using ScoringService.Domain.Entities;
using ScoringService.Domain.Enums;
using SharedKernel.Base;
using SharedKernel.Events;

namespace ScoringService.Application.EventHandlers;

/// <summary>
/// Обработчик события LeadEnriched
/// </summary>
public class LeadEnrichedEventHandler(
    IUnitOfWork unitOfWork,
    ILogger<LeadEnrichedEventHandler> logger)
    : INotificationHandler<IntegrationEventWrapper<LeadEnrichedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventWrapper<LeadEnrichedIntegrationEvent> wrapper, CancellationToken cancellationToken)
    {
        var @event = wrapper.Event;
        logger.LogInformation("Processing LeadEnriched for lead {LeadId}", @event.LeadId);

        var enrichedData = new
        {
            @event.Industry,
            @event.CompanySize,
            @event.Website,
            @event.RevenueRange,
            @event.Version
        };

        var enrichedDataJson = JsonSerializer.Serialize(enrichedData, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var scoringRequest = await unitOfWork.Set<ScoringRequest>()
            .FirstOrDefaultAsync(x => x.LeadId == @event.LeadId, cancellationToken);

        if (scoringRequest != null)
        {
            if (scoringRequest.Status != ScoringRequestStatus.Completed)
            {
                scoringRequest.UpdateEnrichedData(enrichedDataJson);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                logger.LogInformation(
                    "Updated enriched data for scoring request of lead {LeadId} (Status: {Status}). Industry: {Industry}, CompanySize: {CompanySize}",
                    @event.LeadId,
                    scoringRequest.Status,
                    @event.Industry,
                    @event.CompanySize);
            }
            else
            {
                logger.LogInformation(
                    "Lead {LeadId} scoring request already completed. Ignoring late enriched data.",
                    @event.LeadId);
            }
        }
        else
        {
            var pendingData = PendingEnrichedData.Create(@event.LeadId, enrichedDataJson);
            await unitOfWork.Set<PendingEnrichedData>().AddAsync(pendingData, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Stored pending enriched data for lead {LeadId}. Will be used when scoring request is created",
                @event.LeadId);
        }
    }
}