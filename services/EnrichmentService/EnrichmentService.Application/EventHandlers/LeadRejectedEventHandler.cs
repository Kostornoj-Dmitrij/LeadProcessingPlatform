using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AvroSchemas.Messages.LeadEvents;
using EnrichmentService.Domain.Entities;
using SharedKernel.Base;

namespace EnrichmentService.Application.EventHandlers;

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
        logger.LogInformation("Processing LeadRejected for lead {LeadId}", @event.LeadId);

        var enrichment = await unitOfWork.Set<EnrichmentResult>()
            .FirstOrDefaultAsync(x => x.LeadId == @event.LeadId, cancellationToken);

        var compensationLog = CompensationLog.CreateEnrichmentCompensation(
            @event.LeadId,
            enrichment != null ? "Enrichment data removed due to lead rejection" :
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