using MediatR;
using Microsoft.EntityFrameworkCore;
using IntegrationEvents.ScoringEvents;
using LeadService.Domain.Entities;
using Microsoft.Extensions.Logging;
using SharedKernel.Base;
using SharedKernel.Events;

namespace LeadService.Application.EventHandlers;

/// <summary>
/// Обработчик события LeadScored от Scoring Service
/// </summary>
public class LeadScoredEventHandler(
    IUnitOfWork unitOfWork,
    ILogger<LeadScoredEventHandler> logger)
    : INotificationHandler<IntegrationEventWrapper<LeadScoredIntegrationEvent>>
{
    public async Task Handle(IntegrationEventWrapper<LeadScoredIntegrationEvent> wrapper, CancellationToken cancellationToken)
    {
        var @event = wrapper.Event;

        logger.LogInformation("Processing LeadScored event for LeadId: {LeadId}", @event.LeadId);

        try
        {
            var lead = await unitOfWork.Set<Lead>()
                .FirstOrDefaultAsync(x => x.Id == @event.LeadId, cancellationToken);

            if (lead == null)
            {
                logger.LogWarning("Lead not found for LeadId: {LeadId}", @event.LeadId);
                return;
            }

            lead.MarkScoringReceived(@event.TotalScore);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Successfully processed scoring for lead {LeadId}", lead.Id);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogWarning("Concurrency conflict for lead {LeadId}, will retry", @event.LeadId);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing scoring for lead {LeadId}", @event.LeadId);
            throw;
        }
    }
}