using MediatR;
using Microsoft.EntityFrameworkCore;
using IntegrationEvents.EnrichmentEvents;
using LeadService.Domain.Entities;
using Microsoft.Extensions.Logging;
using SharedKernel.Base;
using SharedKernel.Events;

namespace LeadService.Application.EventHandlers;

/// <summary>
/// Обработчик события LeadEnrichmentCompensated от Enrichment Service
/// </summary>
public class LeadEnrichmentCompensatedEventHandler(
    IUnitOfWork unitOfWork,
    ILogger<LeadEnrichmentCompensatedEventHandler> logger)
    : INotificationHandler<IntegrationEventWrapper<LeadEnrichmentCompensatedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventWrapper<LeadEnrichmentCompensatedIntegrationEvent> wrapper, CancellationToken cancellationToken)
    {
        var @event = wrapper.Event;
        
        logger.LogInformation("Processing LeadEnrichmentCompensated for lead {LeadId}", @event.LeadId);

        try
        {
            var lead = await unitOfWork.Set<Lead>()
                .FirstOrDefaultAsync(x => x.Id == @event.LeadId, cancellationToken);
            
            if (lead == null)
            {
                logger.LogWarning("Lead not found: {LeadId}", @event.LeadId);
                return;
            }
            
            lead.MarkEnrichmentCompensated();
            
            await unitOfWork.SaveChangesAsync(cancellationToken);
            
            logger.LogInformation("Marked enrichment compensation as received for lead {LeadId}", lead.Id);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogWarning("Concurrency conflict for lead {LeadId}, will retry", @event.LeadId);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing enrichment compensation for lead {LeadId}", @event.LeadId);
            throw;
        }
    }
}