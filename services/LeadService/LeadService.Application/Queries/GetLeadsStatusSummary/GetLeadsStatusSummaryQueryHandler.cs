using LeadService.Application.Common.DTOs;
using LeadService.Domain.Entities;
using LeadService.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Base;

namespace LeadService.Application.Queries.GetLeadsStatusSummary;

/// <summary>
/// Обработчик запроса на получение сводки по статусам списка лидов
/// </summary>
public class GetLeadsStatusSummaryQueryHandler(IUnitOfWork unitOfWork) 
    : IRequestHandler<GetLeadsStatusSummaryQuery, LeadsStatusSummaryDto>
{
    public async Task<LeadsStatusSummaryDto> Handle(
        GetLeadsStatusSummaryQuery request, 
        CancellationToken cancellationToken)
    {
        if (request.LeadIds.Count == 0)
        {
            return new LeadsStatusSummaryDto();
        }

        var statusGroups = await unitOfWork.Set<Lead>()
            .Where(l => request.LeadIds.Contains(l.Id))
            .GroupBy(l => l.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var result = new LeadsStatusSummaryDto
        {
            Total = request.LeadIds.Count
        };

        foreach (var group in statusGroups)
        {
            switch (group.Status)
            {
                case LeadStatus.Closed:
                    result.Closed = group.Count;
                    break;
                case LeadStatus.Rejected:
                    result.Rejected = group.Count;
                    break;
                case LeadStatus.FailedDistribution:
                    result.FailedDistribution = group.Count;
                    break;
                case LeadStatus.Initial:
                case LeadStatus.Qualified:
                case LeadStatus.Distributed:
                    result.InProgress += group.Count;
                    break;
            }
        }

        var foundCount = statusGroups.Sum(g => g.Count);
        result.InProgress += (result.Total - foundCount);

        return result;
    }
}