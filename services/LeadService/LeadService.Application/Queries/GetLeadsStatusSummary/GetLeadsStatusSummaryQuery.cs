using LeadService.Application.Common.DTOs;
using MediatR;

namespace LeadService.Application.Queries.GetLeadsStatusSummary;

/// <summary>
/// Запрос на получение сводки по статусам списка лидов
/// </summary>
public class GetLeadsStatusSummaryQuery : IRequest<LeadsStatusSummaryDto>
{
    public List<Guid> LeadIds { get; init; } = [];
}