using MediatR;
using Microsoft.EntityFrameworkCore;
using IntegrationEvents.EnrichmentEvents;
using LeadService.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using SharedKernel.Base;
using System.Text.Json;
using SharedKernel.Events;

namespace LeadService.Application.EventHandlers;

/// <summary>
/// Обработчик события LeadEnriched от Enrichment Service
/// </summary>
public class LeadEnrichedEventHandler(
    IApplicationDbContext context,
    IUnitOfWork unitOfWork,
    ILogger<LeadEnrichedEventHandler> logger)
    : INotificationHandler<IntegrationEventWrapper<LeadEnrichedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventWrapper<LeadEnrichedIntegrationEvent> wrapper, CancellationToken cancellationToken)
    {
        var @event = wrapper.Event;
        
        logger.LogInformation("Processing LeadEnriched for lead {LeadId}", @event.LeadId);

        try
        {
            var lead = await context.Leads
                .FirstOrDefaultAsync(x => x.Id == @event.LeadId, cancellationToken);

            if (lead == null)
            {
                logger.LogWarning("Lead not found for LeadId: {LeadId}", @event.LeadId);
                return;
            }

            var enrichedDataJson = JsonSerializer.Serialize(new
            {
                @event.Industry,
                @event.CompanySize,
                @event.Website,
                @event.RevenueRange,
                @event.Version
            });
            
            lead.MarkEnrichmentReceived(enrichedDataJson);
        
            await unitOfWork.SaveChangesAsync(cancellationToken);
            
            logger.LogInformation("Successfully processed enrichment for lead {LeadId}", lead.Id);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogWarning("Concurrency conflict for lead {LeadId}, will retry", @event.LeadId);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing enrichment for lead {LeadId}", @event.LeadId);
            throw;
        }
    }
}