using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using SharedKernel.Base;

namespace LeadService.Tests.Common.Database;

/// <summary>
/// Базовый класс для тестов, работающих с базой данных
/// </summary>
public abstract class DatabaseTestBase
{
    public Mock<IUnitOfWork> UnitOfWorkMock = null!;

    [SetUp]
    public virtual void BaseSetup()
    {
        UnitOfWorkMock = new Mock<IUnitOfWork>();
    }

    [TearDown]
    public virtual void BaseCleanup()
    {
        UnitOfWorkMock.Reset();
    }

    protected static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data) where T : class
    {
        var queryable = data.AsQueryable();
        var mockSet = new Mock<DbSet<T>>();

        mockSet.As<IQueryable<T>>().Setup(m => m.Provider)
            .Returns(new MockAsyncQueryProvider<T>(queryable.Provider));
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());

        mockSet.As<IAsyncEnumerable<T>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new MockAsyncEnumerator<T>(data.GetEnumerator()));

        return mockSet;
    }
}