using MediatR;
using Microsoft.EntityFrameworkCore;
using IntegrationEvents.DistributionEvents;
using LeadService.Domain.Entities;
using Microsoft.Extensions.Logging;
using SharedKernel.Base;
using SharedKernel.Events;

namespace LeadService.Application.EventHandlers;

/// <summary>
/// Обработчик события DistributionSucceeded от Distribution Service
/// </summary>
public class DistributionSucceededEventHandler(
    IUnitOfWork unitOfWork,
    ILogger<DistributionSucceededEventHandler> logger)
    : INotificationHandler<IntegrationEventWrapper<DistributionSucceededIntegrationEvent>>
{
    public async Task Handle(IntegrationEventWrapper<DistributionSucceededIntegrationEvent> wrapper, CancellationToken cancellationToken)
    {
        var @event = wrapper.Event;
        
        logger.LogInformation("Processing DistributionSucceeded for lead {LeadId}", @event.LeadId);

        try
        {
            var lead = await unitOfWork.Set<Lead>()
                .FirstOrDefaultAsync(x => x.Id == @event.LeadId, cancellationToken);
            
            if (lead == null)
            {
                logger.LogWarning("Lead not found: {LeadId}", @event.LeadId);
                return;
            }
            
            lead.MarkAsDistributed(@event.Target);
            
            await unitOfWork.SaveChangesAsync(cancellationToken);
            
            logger.LogInformation("Lead {LeadId} marked as distributed", lead.Id);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogWarning("Concurrency conflict for lead {LeadId}, will retry", @event.LeadId);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing distribution success for lead {LeadId}", @event.LeadId);
            throw;
        }
    }
}