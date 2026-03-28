using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AvroSchemas.Messages.LeadEvents;
using ScoringService.Domain.Entities;
using SharedKernel.Base;

namespace ScoringService.Application.EventHandlers;

/// <summary>
/// Обработчик события LeadDistributionFailed
/// </summary>
public class LeadDistributionFailedEventHandler(
    IUnitOfWork unitOfWork,
    ILogger<LeadDistributionFailedEventHandler> logger)
    : INotificationHandler<LeadDistributionFailed>
{
    public async Task Handle(LeadDistributionFailed @event, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing LeadDistributionFailed for lead {LeadId}", @event.LeadId);

        var scoringResult = await unitOfWork.Set<ScoringResult>()
            .FirstOrDefaultAsync(x => x.LeadId == @event.LeadId, cancellationToken);

        var scoringRequest = await unitOfWork.Set<ScoringRequest>()
            .FirstOrDefaultAsync(x => x.LeadId == @event.LeadId, cancellationToken);

        var compensationLog = CompensationLog.CreateScoringCompensation(
            @event.LeadId,
            scoringResult != null 
                ? $"Scoring result removed due to distribution failure. Score: {scoringResult.TotalScore}" 
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