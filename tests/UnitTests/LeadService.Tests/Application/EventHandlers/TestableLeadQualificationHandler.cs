using LeadService.Application.EventHandlers;
using LeadService.Domain.Entities;
using Microsoft.Extensions.Logging;
using SharedKernel.Base;

namespace LeadService.Tests.Application.EventHandlers;

/// <summary>
/// Тестовая реализация LeadQualificationHandler для подмены GetLeadForUpdateAsync
/// </summary>
public class TestableLeadQualificationHandler(
    IUnitOfWork unitOfWork,
    ILogger<LeadQualificationHandler> logger,
    Func<Guid, CancellationToken, Task<Lead?>> getLeadFunc)
    : LeadQualificationHandler(unitOfWork, logger)
{
    protected override Task<Lead?> GetLeadForUpdateAsync(Guid leadId, CancellationToken cancellationToken)
    {
        return getLeadFunc(leadId, cancellationToken);
    }
}