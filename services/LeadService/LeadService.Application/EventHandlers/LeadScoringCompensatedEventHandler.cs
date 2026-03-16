using MediatR;
using Microsoft.EntityFrameworkCore;
using IntegrationEvents.ScoringEvents;
using LeadService.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using SharedKernel.Base;
using SharedKernel.Events;

namespace LeadService.Application.EventHandlers;

/// <summary>
/// Обработчик события LeadScoringCompensated от Scoring Service
/// </summary>
public class LeadScoringCompensatedEventHandler(
    IApplicationDbContext context,
    IUnitOfWork unitOfWork,
    ILogger<LeadScoringCompensatedEventHandler> logger)
    : INotificationHandler<IntegrationEventWrapper<LeadScoringCompensatedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventWrapper<LeadScoringCompensatedIntegrationEvent> wrapper, CancellationToken cancellationToken)
    {
        var @event = wrapper.Event;
        
        logger.LogInformation("Processing LeadScoringCompensated for lead {LeadId}", @event.LeadId);

        try
        {
            var lead = await context.Leads
                .FirstOrDefaultAsync(x => x.Id == @event.LeadId, cancellationToken);
            
            if (lead == null)
            {
                logger.LogWarning("Lead not found: {LeadId}", @event.LeadId);
                return;
            }
            
            lead.MarkScoringCompensated();
            
            await unitOfWork.SaveChangesAsync(cancellationToken);
            
            logger.LogInformation("Marked scoring compensation as received for lead {LeadId}", lead.Id);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogWarning("Concurrency conflict for lead {LeadId}, will retry", @event.LeadId);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing scoring compensation for lead {LeadId}", @event.LeadId);
            throw;
        }
    }
}