using MediatR;
using Microsoft.Extensions.Logging;
using EnrichmentService.Domain.Entities;
using IntegrationEvents.LeadEvents;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Base;
using SharedKernel.Events;

namespace EnrichmentService.Application.EventHandlers;

/// <summary>
/// Обработчик события LeadCreated
/// </summary>
public class LeadCreatedEventHandler(
    IUnitOfWork unitOfWork,
    ILogger<LeadCreatedEventHandler> logger)
    : INotificationHandler<IntegrationEventWrapper<LeadCreatedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventWrapper<LeadCreatedIntegrationEvent> wrapper,
        CancellationToken cancellationToken)
    {
        var @event = wrapper.Event;
        logger.LogInformation("Processing LeadCreated for lead {LeadId}", @event.LeadId);

        var existingRequest = await unitOfWork.Set<EnrichmentRequest>()
            .FirstOrDefaultAsync(x => x.LeadId == @event.LeadId, cancellationToken);
        if (existingRequest != null)
        {
            logger.LogInformation("Lead {LeadId} already has an enrichment request in status {Status}, skipping", 
                @event.LeadId, existingRequest.Status);
            return;
        }

        var request = EnrichmentRequest.Create(
            leadId: @event.LeadId,
            companyName: @event.CompanyName,
            email: @event.Email,
            contactPerson: @event.ContactPerson,
            customFields: @event.CustomFields);

        await unitOfWork.Set<EnrichmentRequest>().AddAsync(request, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Enrichment request created for lead {LeadId}", @event.LeadId);
    }
}