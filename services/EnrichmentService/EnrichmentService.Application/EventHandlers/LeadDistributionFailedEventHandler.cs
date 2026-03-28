using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AvroSchemas.Messages.LeadEvents;
using EnrichmentService.Domain.Entities;
using SharedKernel.Base;

namespace EnrichmentService.Application.EventHandlers;

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

        var enrichment = await unitOfWork.Set<EnrichmentResult>()
            .FirstOrDefaultAsync(x => x.LeadId == @event.LeadId, cancellationToken);

        var compensationLog = CompensationLog.CreateEnrichmentCompensation(
            @event.LeadId,
            enrichment != null ? "Enrichment data removed due to distribution failure" :
                "No enrichment data found, compensated anyway");
        await unitOfWork.Set<CompensationLog>().AddAsync(compensationLog, cancellationToken);

        if (enrichment != null)
        {
            unitOfWork.Set<EnrichmentResult>().Remove(enrichment);
        }

        compensationLog.MarkCompensated();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}