using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LeadService.Application.Common.DTOs;
using LeadService.Application.Common.Interfaces;
using LeadService.Application.Metrics;
using LeadService.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedInfrastructure.Telemetry;
using SharedKernel.Base;
using SharedKernel.Json;

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
    public async Task<LeadDto> Handle(CreateLeadCommand request, CancellationToken cancellationToken)
    {
        TelemetryContext.SetLeadBaggage(Guid.NewGuid(), "LeadCreation");

        using var activity = ActivityBuilderExtensions.CreateCommandActivity("CreateLead")
            .WithTags(
                (TelemetryAttributes.LeadSource, request.Source),
                (TelemetryAttributes.LeadCompany, request.CompanyName),
                (TelemetryAttributes.LeadEmail, request.Email),
                (TelemetryAttributes.LeadExternalId, request.ExternalLeadId ?? "none"),
                (TelemetryAttributes.LeadHasCustomFields, request.CustomFields?.Count > 0));

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

            var cachedResponse = JsonSerializer.Deserialize<LeadDto>(
                idempotencyKey.ResponseBody!,
                JsonDefaults.Options);
            if (cachedResponse == null || cachedResponse.Id == Guid.Empty)
            {
                logger.LogWarning("Cached response for key {Key} is invalid, reprocessing",
                    request.ExternalLeadId);
                var result = await CreateLeadInternal(request, cancellationToken);
                await idempotencyRepository.UpdateWithResultAsync(
                    idempotencyKey.Id,
                    result.Id,
                    200,
                    JsonSerializer.Serialize(result, JsonDefaults.Options),
                    cancellationToken);
                return result;
            }

            return cachedResponse;
        }

        try
        {
            var result = await CreateLeadInternal(request, cancellationToken);

            await idempotencyRepository.UpdateWithResultAsync(
                idempotencyKey.Id,
                result.Id,
                200,
                JsonSerializer.Serialize(result, JsonDefaults.Options),
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
        var stopwatch = Stopwatch.StartNew();

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

        await unitOfWork.SaveChangesAsync(cancellationToken);

        LeadMetrics.LeadsCreated.Add(1, new TagList { { "source", request.Source } });
        LeadMetrics.CommandHandlingDuration.Record(stopwatch.Elapsed.TotalMilliseconds, 
            new TagList { { "command_name", "CreateLead" } });

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
        }, JsonDefaults.Options);

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