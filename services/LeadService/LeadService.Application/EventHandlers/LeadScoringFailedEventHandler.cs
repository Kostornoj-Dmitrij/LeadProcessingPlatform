using MediatR;
using Microsoft.EntityFrameworkCore;
using IntegrationEvents.ScoringEvents;
using LeadService.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using SharedKernel.Base;
using SharedKernel.Events;

namespace LeadService.Application.EventHandlers;

/// <summary>
/// Обработчик события LeadScoringFailed от Scoring Service
/// </summary>
public class LeadScoringFailedEventHandler(
    IApplicationDbContext context,
    IUnitOfWork unitOfWork,
    ILogger<LeadScoringFailedEventHandler> logger)
    : INotificationHandler<IntegrationEventWrapper<LeadScoringFailedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventWrapper<LeadScoringFailedIntegrationEvent> wrapper, CancellationToken cancellationToken)
    {
        var @event = wrapper.Event;
        
        logger.LogInformation("Processing LeadScoringFailed for lead {LeadId}", @event.LeadId);

        try
        {
            var lead = await context.Leads
                .FirstOrDefaultAsync(x => x.Id == @event.LeadId, cancellationToken);
            
            if (lead == null)
            {
                logger.LogWarning("Lead not found: {LeadId}", @event.LeadId);
                return;
            }
            
            lead.Reject($"Scoring failed: {@event.Reason}", "ScoringFailed");
            
            await unitOfWork.SaveChangesAsync(cancellationToken);
            
            logger.LogInformation("Lead {LeadId} rejected due to scoring failure. Reason: {Reason}", 
                lead.Id, @event.Reason);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogWarning("Concurrency conflict for lead {LeadId}, will retry", @event.LeadId);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing scoring failure for lead {LeadId}", @event.LeadId);
            throw;
        }
    }
}