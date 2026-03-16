using MediatR;
using LeadService.Application.DTOs;

namespace LeadService.Application.Queries.GetLeadById;

/// <summary>
/// Запрос на получение лида по идентификатору
/// </summary>
public class GetLeadByIdQuery : IRequest<LeadDto?>
{
    public Guid Id { get; init; }
}