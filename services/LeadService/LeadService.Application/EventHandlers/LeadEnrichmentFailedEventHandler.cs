using MediatR;
using Microsoft.EntityFrameworkCore;
using IntegrationEvents.EnrichmentEvents;
using LeadService.Domain.Entities;
using Microsoft.Extensions.Logging;
using SharedKernel.Base;
using SharedKernel.Events;

namespace LeadService.Application.EventHandlers;

/// <summary>
/// Обработчик события LeadEnrichmentFailed от Enrichment Service
/// </summary>
public class LeadEnrichmentFailedEventHandler(
    IUnitOfWork unitOfWork,
    ILogger<LeadEnrichmentFailedEventHandler> logger)
    : INotificationHandler<IntegrationEventWrapper<LeadEnrichmentFailedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventWrapper<LeadEnrichmentFailedIntegrationEvent> wrapper, CancellationToken cancellationToken)
    {
        var @event = wrapper.Event;
        
        logger.LogInformation("Processing LeadEnrichmentFailed for lead {LeadId}", @event.LeadId);

        try
        {
            var lead = await unitOfWork.Set<Lead>()
                .FirstOrDefaultAsync(x => x.Id == @event.LeadId, cancellationToken);
            
            if (lead == null)
            {
                logger.LogWarning("Lead not found: {LeadId}", @event.LeadId);
                return;
            }
            
            lead.Reject($"Enrichment failed: {@event.Reason}", "EnrichmentFailed");
            
            await unitOfWork.SaveChangesAsync(cancellationToken);
            
            logger.LogInformation("Lead {LeadId} rejected due to enrichment failure", lead.Id);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogWarning("Concurrency conflict for lead {LeadId}, will retry", @event.LeadId);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing enrichment failure for lead {LeadId}", @event.LeadId);
            throw;
        }
    }
}