using MediatR;
using Microsoft.EntityFrameworkCore;
using IntegrationEvents.DistributionEvents;
using LeadService.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using SharedKernel.Base;
using SharedKernel.Events;

namespace LeadService.Application.EventHandlers;

/// <summary>
/// Обработчик события DistributionFailed от Distribution Service
/// </summary>
public class DistributionFailedEventHandler(
    IApplicationDbContext context,
    IUnitOfWork unitOfWork,
    ILogger<DistributionFailedEventHandler> logger)
    : INotificationHandler<IntegrationEventWrapper<DistributionFailedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventWrapper<DistributionFailedIntegrationEvent> wrapper, CancellationToken cancellationToken)
    {
        var @event = wrapper.Event;
        
        logger.LogInformation("Processing DistributionFailed for lead {LeadId}", @event.LeadId);

        try
        {
            var lead = await context.Leads
                .FirstOrDefaultAsync(x => x.Id == @event.LeadId, cancellationToken);
            
            if (lead == null)
            {
                logger.LogWarning("Lead not found: {LeadId}", @event.LeadId);
                return;
            }
            
            lead.MarkDistributionFailed(@event.Reason);
            
            await unitOfWork.SaveChangesAsync(cancellationToken);
            
            logger.LogInformation("Lead {LeadId} marked as distribution failed", lead.Id);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogWarning("Concurrency conflict for lead {LeadId}, will retry", @event.LeadId);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing distribution failure for lead {LeadId}", @event.LeadId);
            throw;
        }
    }
}