using AvroSchemas.Messages.LeadEvents;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScoringService.Domain.Entities;
using SharedInfrastructure.Telemetry;
using SharedKernel.Base;

namespace ScoringService.Application.EventHandlers;

/// <summary>
/// Обработчик события LeadRejected
/// </summary>
public class LeadRejectedEventHandler(
    IUnitOfWork unitOfWork,
    ILogger<LeadRejectedEventHandler> logger)
    : INotificationHandler<LeadRejected>
{
    public async Task Handle(LeadRejected @event, CancellationToken cancellationToken)
    {
        using var activity = ActivityBuilderExtensions.CreateEventActivity(@event)
            .WithFailureTags(reason: @event.Reason, failureType: @event.FailureType)
            .WithProcessingStep("scoring_compensation");

        logger.LogInformation("Processing LeadRejected for lead {LeadId}", @event.LeadId);

        var scoringResult = await unitOfWork.Set<ScoringResult>()
            .FirstOrDefaultAsync(x => x.LeadId == @event.LeadId, cancellationToken);

        var scoringRequest = await unitOfWork.Set<ScoringRequest>()
            .FirstOrDefaultAsync(x => x.LeadId == @event.LeadId, cancellationToken);

        var compensationLog = CompensationLog.CreateScoringCompensation(
            @event.LeadId,
            scoringResult != null
                ? $"Scoring result removed due to lead rejection. Reason: {@event.Reason}"
                : "No scoring result found, compensated anyway");

        await unitOfWork.Set<CompensationLog>().AddAsync(compensationLog, cancellationToken);

        if (scoringResult != null)
        {
            unitOfWork.Set<ScoringResult>().Remove(scoringResult);
            logger.LogInformation("Removed scoring result for lead {LeadId}", @event.LeadId);
        }

        if (scoringRequest != null)
        {
            if (scoringRequest.EnrichedData != null)
            {
                scoringRequest.ClearEnrichedData();
                logger.LogInformation("Cleared enriched data for scoring request of lead {LeadId}", @event.LeadId);
            }

            unitOfWork.Set<ScoringRequest>().Remove(scoringRequest);
            logger.LogInformation("Removed scoring request for lead {LeadId}", @event.LeadId);
        }

        compensationLog.MarkCompensated();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Compensation completed for lead {LeadId}", @event.LeadId);
    }
}