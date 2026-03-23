using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using IntegrationEvents.EnrichmentEvents;
using IntegrationEvents.ScoringEvents;
using LeadService.Domain.Entities;
using LeadService.Domain.Events;
using LeadService.Domain.Enums;
using SharedKernel.Base;
using SharedKernel.Events;
using SharedKernel.Json;

namespace LeadService.Application.EventHandlers;

/// <summary>
/// Обработчик событий LeadEnriched и LeadScored
/// </summary>
public class LeadQualificationHandler(
    IUnitOfWork unitOfWork,
    ILogger<LeadQualificationHandler> logger)
    :
        INotificationHandler<IntegrationEventWrapper<LeadEnrichedIntegrationEvent>>,
        INotificationHandler<IntegrationEventWrapper<LeadScoredIntegrationEvent>>
{
    public async Task Handle(IntegrationEventWrapper<LeadEnrichedIntegrationEvent> wrapper, CancellationToken cancellationToken)
    {
        var @event = wrapper.Event;
        logger.LogInformation("Processing LeadEnriched for lead {LeadId}", @event.LeadId);
        await ProcessEvent(@event.LeadId, isEnriched: true, enrichedEvent: @event, scoredEvent: null, cancellationToken);
    }

    public async Task Handle(IntegrationEventWrapper<LeadScoredIntegrationEvent> wrapper, CancellationToken cancellationToken)
    {
        var @event = wrapper.Event;
        logger.LogInformation("Processing LeadScored for lead {LeadId}", @event.LeadId);
        await ProcessEvent(@event.LeadId, isEnriched: false, enrichedEvent: null, scoredEvent: @event, cancellationToken);
    }

    private async Task ProcessEvent(
        Guid leadId,
        bool isEnriched,
        LeadEnrichedIntegrationEvent? enrichedEvent,
        LeadScoredIntegrationEvent? scoredEvent,
        CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var lead = await unitOfWork.Set<Lead>()
                .FromSqlRaw("SELECT * FROM leads WHERE id = {0} FOR UPDATE", leadId)
                .FirstOrDefaultAsync(cancellationToken);

            if (lead == null)
            {
                logger.LogWarning("Lead {LeadId} not found", leadId);
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return;
            }

            if (lead.Status == LeadStatus.Closed)
            {
                logger.LogInformation("Lead {LeadId} is already closed. Ignoring late event.", leadId);
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return;
            }

            if (lead.Status != LeadStatus.Initial)
            {
                logger.LogWarning(
                    "Cannot process event for lead {LeadId} in status {Status}. Ignoring.",
                    leadId, lead.Status);
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return;
            }

            bool wasChanged = false;

            if (isEnriched && enrichedEvent != null && !lead.IsEnrichmentReceived)
            {
                var enrichedData = new EnrichedDataDto
                {
                    Industry = enrichedEvent.Industry,
                    CompanySize = enrichedEvent.CompanySize,
                    Website = enrichedEvent.Website,
                    RevenueRange = enrichedEvent.RevenueRange,
                    Version = enrichedEvent.Version
                };
                var enrichedDataJson = JsonSerializer.Serialize(enrichedData, JsonDefaults.Options);
                
                lead.MarkEnrichmentReceived(enrichedDataJson);
                wasChanged = true;
                logger.LogDebug("Applied enrichment to lead {LeadId}", leadId);
            }
            
            if (!isEnriched && scoredEvent != null && !lead.IsScoringReceived)
            {
                lead.MarkScoringReceived(scoredEvent.TotalScore);
                wasChanged = true;
                logger.LogDebug("Applied scoring to lead {LeadId}", leadId);
            }

            if (wasChanged)
            {
                lead.TryQualify();
                
                await unitOfWork.SaveChangesAsync(cancellationToken);
                await unitOfWork.CommitTransactionAsync(cancellationToken);
                
                logger.LogInformation(
                    "Successfully processed {EventType} for lead {LeadId}. Status: {Status}, EnrichmentReceived: {Enrichment}, ScoringReceived: {Scoring}",
                    isEnriched ? "enrichment" : "scoring",
                    lead.Id,
                    lead.Status,
                    lead.IsEnrichmentReceived,
                    lead.IsScoringReceived);
            }
            else
            {
                await unitOfWork.CommitTransactionAsync(cancellationToken);
                logger.LogDebug("No changes applied for lead {LeadId}", leadId);
            }
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict processing event for lead {LeadId}. Transaction rolled back.", leadId);
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing event for lead {LeadId}. Transaction rolled back.", leadId);
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}