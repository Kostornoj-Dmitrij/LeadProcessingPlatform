using Microsoft.EntityFrameworkCore;
using LeadService.Domain.Entities;
using SharedKernel.Entities;

namespace LeadService.Application.Common.Interfaces;

/// <summary>
/// Интерфейс контекста базы данных для сервиса лидов
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Lead> Leads { get; }
    DbSet<LeadCustomField> LeadCustomFields { get; }
    
    DbSet<IdempotencyKey> IdempotencyKeys { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}