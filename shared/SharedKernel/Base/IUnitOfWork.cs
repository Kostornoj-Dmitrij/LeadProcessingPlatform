using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace SharedKernel.Base;

/// <summary>
/// Интерфейс для паттерна Unit of Work.
/// Позволяет группировать несколько операций в одну транзакцию.
/// </summary>
public interface IUnitOfWork
{
    DbSet<T> Set<T>() where T : class;

    EntityEntry<T> Entry<T>(T entity) where T : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}