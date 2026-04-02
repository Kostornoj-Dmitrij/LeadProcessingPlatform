using AvroSchemas.Messages.ScoringEvents;
using LeadService.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedInfrastructure.Telemetry;
using SharedKernel.Base;

namespace LeadService.Application.EventHandlers;

/// <summary>
/// Обработчик события LeadScoringCompensated от Scoring Service
/// </summary>
public class LeadScoringCompensatedEventHandler(
    IUnitOfWork unitOfWork,
    ILogger<LeadScoringCompensatedEventHandler> logger)
    : INotificationHandler<LeadScoringCompensated>
{
    public async Task Handle(LeadScoringCompensated @event, CancellationToken cancellationToken)
    {
        using var activity = TelemetryConstants.ActivitySource.StartEventHandlerSpan("LeadScoringCompensated")!
            .AddTags(
                (TelemetryAttributes.LeadId, @event.LeadId),
                (TelemetryAttributes.EventName, "LeadScoringCompensated"),
                (TelemetryAttributes.ProcessingStep, "scoring_compensation"));
        logger.LogInformation("Processing LeadScoringCompensated for lead {LeadId}", @event.LeadId);

        try
        {
            var lead = await unitOfWork.Set<Lead>()
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