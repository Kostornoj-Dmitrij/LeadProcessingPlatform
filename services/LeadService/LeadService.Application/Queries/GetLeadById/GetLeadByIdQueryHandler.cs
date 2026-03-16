using MediatR;
using Microsoft.EntityFrameworkCore;
using LeadService.Application.Common.Interfaces;
using LeadService.Application.DTOs;

namespace LeadService.Application.Queries.GetLeadById;

/// <summary>
/// Обработчик запроса на получение лида по идентификатору
/// </summary>
public class GetLeadByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetLeadByIdQuery, LeadDto?>
{
    public async Task<LeadDto?> Handle(GetLeadByIdQuery request, CancellationToken cancellationToken)
    {
        var lead = await context.Leads
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