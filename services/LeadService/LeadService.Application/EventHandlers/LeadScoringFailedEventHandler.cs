using System.Diagnostics;
using AvroSchemas.Messages.ScoringEvents;
using LeadService.Application.Metrics;
using LeadService.Domain.Constants;
using LeadService.Domain.Entities;
using LeadService.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedInfrastructure.Telemetry;
using SharedKernel.Base;

namespace LeadService.Application.EventHandlers;

/// <summary>
/// Обработчик события LeadScoringFailed от Scoring Service
/// </summary>
public class LeadScoringFailedEventHandler(
    IUnitOfWork unitOfWork,
    ILogger<LeadScoringFailedEventHandler> logger)
    : INotificationHandler<LeadScoringFailed>
{
    public async Task Handle(LeadScoringFailed @event, CancellationToken cancellationToken)
    {
        using var activity = ActivityBuilderExtensions.CreateEventActivity(@event)
            .WithFailureTags(@event.Reason, @event.RetryCount, FailureTypeConstants.ScoringFailed)
            .WithProcessingStep("scoring_failure_handling");

        logger.LogInformation("Processing LeadScoringFailed for lead {LeadId}", @event.LeadId);

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
                    "Lead {LeadId} is already closed. Ignoring late LeadScoringFailed event.",
                    lead.Id);
                return;
            }

            if (lead.Status != LeadStatus.Initial)
            {
                logger.LogWarning(
                    "Cannot reject lead {LeadId} from status {Status} due to scoring failure. Ignoring.",
                    lead.Id,
                    lead.Status);
                return;
            }

            lead.Reject($"Scoring failed: {@event.Reason}", FailureTypeConstants.ScoringFailed);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            LeadMetrics.LeadsRejected.Add(1, new TagList { { "failure_type", "ScoringFailed" } });
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