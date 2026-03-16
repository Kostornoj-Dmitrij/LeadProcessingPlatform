using MediatR;
using LeadService.Application.DTOs;
using LeadService.Domain.Enums;

namespace LeadService.Application.Queries.GetLeadsByStatus;

/// <summary>
/// Запрос на получение списка лидов по статусу
/// </summary>
public class GetLeadsByStatusQuery : IRequest<List<LeadDto>>
{
    public LeadStatus? Status { get; init; }
    public int? Limit { get; init; }
    public int? Offset { get; init; }
}