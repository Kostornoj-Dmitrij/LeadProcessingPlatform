using AvroSchemas.Messages.EnrichmentEvents;
using LeadService.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedInfrastructure.Telemetry;
using SharedKernel.Base;

namespace LeadService.Application.EventHandlers;

/// <summary>
/// Обработчик события LeadEnrichmentCompensated от Enrichment Service
/// </summary>
public class LeadEnrichmentCompensatedEventHandler(
    IUnitOfWork unitOfWork,
    ILogger<LeadEnrichmentCompensatedEventHandler> logger)
    : INotificationHandler<LeadEnrichmentCompensated>
{
    public async Task Handle(LeadEnrichmentCompensated @event, CancellationToken cancellationToken)
    {
        using var activity = TelemetryConstants.ActivitySource.StartEventHandlerSpan("LeadEnrichmentCompensated")!
            .AddTags(
                (TelemetryAttributes.LeadId, @event.LeadId),
                (TelemetryAttributes.EventName, "LeadEnrichmentCompensated"),
                (TelemetryAttributes.ProcessingStep, "enrichment_compensation"));
        logger.LogInformation("Processing LeadEnrichmentCompensated for lead {LeadId}", @event.LeadId);

        try
        {
            var lead = await unitOfWork.Set<Lead>()
                .FirstOrDefaultAsync(x => x.Id == @event.LeadId, cancellationToken);

            if (lead == null)
            {
                logger.LogWarning("Lead not found: {LeadId}", @event.LeadId);
                return;
            }

            lead.MarkEnrichmentCompensated();

            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Marked enrichment compensation as received for lead {LeadId}", lead.Id);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogWarning("Concurrency conflict for lead {LeadId}, will retry", @event.LeadId);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing enrichment compensation for lead {LeadId}", @event.LeadId);
            throw;
        }
    }
}