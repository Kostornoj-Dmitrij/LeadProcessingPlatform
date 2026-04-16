using System.Diagnostics;
using AvroSchemas.Messages.EnrichmentEvents;
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
/// Обработчик события LeadEnrichmentFailed от Enrichment Service
/// </summary>
public class LeadEnrichmentFailedEventHandler(
    IUnitOfWork unitOfWork,
    ILogger<LeadEnrichmentFailedEventHandler> logger)
    : INotificationHandler<LeadEnrichmentFailed>
{
    public async Task Handle(LeadEnrichmentFailed @event, CancellationToken cancellationToken)
    {
        using var activity = ActivityBuilderExtensions.CreateEventActivity(@event)
            .WithFailureTags(@event.Reason, @event.RetryCount, FailureTypeConstants.EnrichmentFailed)
            .WithProcessingStep("enrichment_failure_handling");

        logger.LogInformation("Processing LeadEnrichmentFailed for lead {LeadId}", @event.LeadId);

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
                    "Lead {LeadId} is already closed. Ignoring late LeadEnrichmentFailed event.",
                    lead.Id);
                return;
            }

            if (lead.Status != LeadStatus.Initial)
            {
                logger.LogWarning(
                    "Cannot reject lead {LeadId} from status {Status} due to enrichment failure. Ignoring.",
                    lead.Id,
                    lead.Status);
                return;
            }

            lead.Reject($"Enrichment failed: {@event.Reason}", FailureTypeConstants.EnrichmentFailed);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            LeadMetrics.LeadsRejected.Add(1, new TagList { { "failure_type", "EnrichmentFailed" } });
            logger.LogInformation("Lead {LeadId} rejected due to enrichment failure", lead.Id);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogWarning("Concurrency conflict for lead {LeadId}, will retry", @event.LeadId);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing enrichment failure for lead {LeadId}", @event.LeadId);
            throw;
        }
    }
}