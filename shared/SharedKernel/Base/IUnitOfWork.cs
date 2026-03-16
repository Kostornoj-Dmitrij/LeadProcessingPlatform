namespace SharedKernel.Base;

/// <summary>
/// Интерфейс для паттерна Unit of Work.
/// Позволяет группировать несколько операций в одну транзакцию.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}