using System.Linq.Expressions;

namespace SharedTestInfrastructure.Database;

/// <summary>
/// Асинхронный перечислитель для мок-коллекций
/// </summary>
public class MockAsyncEnumerable<T>(Expression expression)
    : EnumerableQuery<T>(expression), IAsyncEnumerable<T>, IQueryable<T>
{
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new MockAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    IQueryProvider IQueryable.Provider => new MockAsyncQueryProvider<T>(this);
}