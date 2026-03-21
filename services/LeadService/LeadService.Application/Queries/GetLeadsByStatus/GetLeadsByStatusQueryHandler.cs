using LeadService.Application.Common.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LeadService.Domain.Entities;
using SharedKernel.Base;

namespace LeadService.Application.Queries.GetLeadsByStatus;

/// <summary>
/// Обработчик запроса на получение списка лидов с фильтрацией по статусу
/// </summary>
public class GetLeadsByStatusQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetLeadsByStatusQuery, List<LeadDto>>
{
    public async Task<List<LeadDto>> Handle(GetLeadsByStatusQuery request, CancellationToken cancellationToken)
    {
        var query = unitOfWork.Set<Lead>()
            .Include(x => x.CustomFields)
            .AsQueryable();

        if (request.Status.HasValue)
        {
            query = query.Where(x => x.Status == request.Status.Value);
        }

        if (request.Offset.HasValue)
        {
            query = query.Skip(request.Offset.Value);
        }

        if (request.Limit.HasValue)
        {
            query = query.Take(request.Limit.Value);
        }

        var leads = await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return leads.Select(lead => new LeadDto
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
        }).ToList();
    }
}