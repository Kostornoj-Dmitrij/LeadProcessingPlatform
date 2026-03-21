using LeadService.Application.Common.DTOs;
using MediatR;
using LeadService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Base;

namespace LeadService.Application.Queries.GetLeadById;

/// <summary>
/// Обработчик запроса на получение лида по идентификатору
/// </summary>
public class GetLeadByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetLeadByIdQuery, LeadDto?>
{
    public async Task<LeadDto?> Handle(GetLeadByIdQuery request, CancellationToken cancellationToken)
    {
        var lead = await unitOfWork.Set<Lead>()
            .Include(x => x.CustomFields)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (lead == null)
            return null;

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