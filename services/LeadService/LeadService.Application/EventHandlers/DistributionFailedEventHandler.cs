using MediatR;
using Microsoft.EntityFrameworkCore;
using IntegrationEvents.DistributionEvents;
using LeadService.Domain.Entities;
using LeadService.Domain.Enums;
using Microsoft.Extensions.Logging;
using SharedKernel.Base;
using SharedKernel.Events;

namespace LeadService.Application.EventHandlers;

/// <summary>
/// Обработчик события DistributionFailed от Distribution Service
/// </summary>
public class DistributionFailedEventHandler(
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
            var lead = await unitOfWork.Set<Lead>()
                .FirstOrDefaultAsync(x => x.Id == @event.LeadId, cancellationToken);

            if (lead == null)
            {
                logger.LogWarning("Lead not found: {LeadId}", @event.LeadId);
                return;
            }

            if (lead.Status == LeadStatus.Closed)
            {
                logger.LogInformation(
                    "Lead {LeadId} is already closed. Ignoring late DistributionFailed event.",
                    lead.Id);
                return;
            }

            if (lead.Status != LeadStatus.Qualified)
            {
                logger.LogWarning(
                    "Cannot mark distribution failed for lead {LeadId} from status {Status}. Ignoring.",
                    lead.Id,
                    lead.Status);
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