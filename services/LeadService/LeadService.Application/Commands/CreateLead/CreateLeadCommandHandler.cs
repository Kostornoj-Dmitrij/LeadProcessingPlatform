using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using MediatR;
using LeadService.Application.Common.Interfaces;
using LeadService.Application.DTOs;
using LeadService.Domain.Entities;
using IntegrationEvents.LeadEvents;
using Microsoft.Extensions.Logging;
using SharedKernel.Base;
using SharedKernel.Entities;

namespace LeadService.Application.Commands.CreateLead;

/// <summary>
/// Обработчик команды создания нового лида
/// </summary>
public class CreateLeadCommandHandler(
    IUnitOfWork unitOfWork,
    IIdempotencyRepository idempotencyRepository,
    ILogger<CreateLeadCommandHandler> logger)
    : IRequestHandler<CreateLeadCommand, LeadDto>
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<LeadDto> Handle(CreateLeadCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.ExternalLeadId))
        {
            return await CreateLeadInternal(request, cancellationToken);
        }

        var requestHash = ComputeRequestHash(request);

        var idempotencyKey = await idempotencyRepository.TryAcquireLockAsync(
            request.ExternalLeadId,
            requestHash,
            TimeSpan.FromSeconds(30),
            cancellationToken);

        if (idempotencyKey == null)
        {
            throw new InvalidOperationException("Request is being processed by another instance");
        }

        if (idempotencyKey.ResponseCode.HasValue)
        {
            logger.LogInformation("Returning cached response for idempotency key {Key}", 
                request.ExternalLeadId);
                
            return JsonSerializer.Deserialize<LeadDto>(idempotencyKey.ResponseBody!, _jsonOptions)!;        }

        try
        {
            var result = await CreateLeadInternal(request, cancellationToken);

            await idempotencyRepository.UpdateWithResultAsync(
                idempotencyKey.Id,
                result.Id,
                200,
                JsonSerializer.Serialize(result),
                cancellationToken);

            return result;
        }
        catch (Exception)
        {
            await idempotencyRepository.ReleaseLockAsync(idempotencyKey.Id, cancellationToken);
            throw;
        }
    }

    private async Task<LeadDto> CreateLeadInternal(CreateLeadCommand request, CancellationToken cancellationToken)
    {
        var lead = Lead.Create(
            id: Guid.NewGuid(),
            source: request.Source,
            companyName: request.CompanyName,
            email: request.Email,
            externalLeadId: request.ExternalLeadId,
            contactPerson: request.ContactPerson,
            phone: request.Phone,
            customFields: request.CustomFields
        );

        await unitOfWork.Set<Lead>().AddAsync(lead, cancellationToken);

        var integrationEvent = new LeadCreatedIntegrationEvent
        {
            LeadId = lead.Id,
            Source = lead.Source,
            CompanyName = lead.CompanyName.Value,
            ContactPerson = lead.ContactPerson,
            Email = lead.Email.Value,
            Phone = lead.Phone?.Value,
            CustomFields = request.CustomFields,
            ExternalLeadId = lead.ExternalLeadId
        };

        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateType = "lead",
            AggregateId = lead.Id.ToString(),
            EventType = integrationEvent.GetType().AssemblyQualifiedName!,
            Payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType()),
            CreatedAt = DateTime.UtcNow,
            ProcessingAttempts = 0
        };

        await unitOfWork.Set<OutboxMessage>().AddAsync(outboxMessage, cancellationToken);

        return MapToDto(lead);
    }

    private static byte[] ComputeRequestHash(CreateLeadCommand request)
    {
        var json = JsonSerializer.Serialize(new
        {
            request.Source,
            request.CompanyName,
            request.Email,
            request.Phone,
            request.ContactPerson,
            request.CustomFields
        });
        
        return SHA256.HashData(Encoding.UTF8.GetBytes(json));
    }

    private static LeadDto MapToDto(Lead lead)
    {
        return new LeadDto
        {
            Id = lead.Id,
            ExternalLeadId = lead.ExternalLeadId,
            Source = lead.Source,
            CompanyName = lead.CompanyName.Value,
            ContactPerson = lead.ContactPerson,
            Email = lead.Email.Value,
            Phone = lead.Phone?.Value,
            Status = lead.Status.ToString(),
            Score = lead.Score,
            CreatedAt = lead.CreatedAt,
            UpdatedAt = lead.UpdatedAt,
            CustomFields = lead.CustomFields.ToDictionary(x => x.FieldName, x => x.FieldValue)
        };
    }
}