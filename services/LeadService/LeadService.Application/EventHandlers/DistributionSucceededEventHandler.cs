using AvroSchemas.Messages.DistributionEvents;
using LeadService.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedInfrastructure.Telemetry;
using SharedKernel.Base;

namespace LeadService.Application.EventHandlers;

/// <summary>
/// Обработчик события DistributionSucceeded от Distribution Service
/// </summary>
public class DistributionSucceededEventHandler(
    IUnitOfWork unitOfWork,
    ILogger<DistributionSucceededEventHandler> logger)
    : INotificationHandler<DistributionSucceeded>
{
    public async Task Handle(DistributionSucceeded @event, CancellationToken cancellationToken)
    {
        using var activity = TelemetryConstants.ActivitySource.StartEventHandlerSpan("DistributionSucceeded")!
            .AddTags(
                (TelemetryAttributes.LeadId, @event.LeadId),
                (TelemetryAttributes.EventName, "DistributionSucceeded"),
                (TelemetryAttributes.DistributionTarget, @event.Target),
                (TelemetryAttributes.DistributionDistributedAt, @event.DistributedAt),
                (TelemetryAttributes.ProcessingStep, "distribution_success_handling"));

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

            lead.CloseAfterDistribution();
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Lead {LeadId} successfully closed after distribution", lead.Id);
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