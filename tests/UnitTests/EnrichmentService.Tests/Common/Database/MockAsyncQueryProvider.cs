using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace EnrichmentService.Tests.Common.Database;

/// <summary>
/// Провайдер для асинхронных запросов к мок-коллекциям
/// </summary>
public class MockAsyncQueryProvider<T>(IQueryProvider inner) : IAsyncQueryProvider
{
    public IQueryable CreateQuery(Expression expression)
    {
        return new MockAsyncEnumerable<T>(expression);
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new MockAsyncEnumerable<TElement>(expression);
    }

    public object? Execute(Expression expression)
    {
        return inner.Execute(expression);
    }

    public TResult Execute<TResult>(Expression expression)
    {
        return inner.Execute<TResult>(expression);
    }

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        var resultType = typeof(TResult).GetGenericArguments()[0];

        if (resultType == typeof(List<T>))
        {
            var items = inner.Execute<IEnumerable<T>>(expression).ToList();
            var task = Task.FromResult(items);
            return (TResult)(object)task;
        }

        var item = inner.Execute<T>(expression);
        var singleTask = Task.FromResult(item);
        return (TResult)(object)singleTask;
    }
}