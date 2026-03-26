using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ScoringService.Domain.Entities;
using SharedKernel.Entities;
using SharedKernel.Events;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using SharedInfrastructure.Inbox;
using SharedInfrastructure.Outbox;
using SharedKernel.Base;

namespace ScoringService.Infrastructure.Data;

/// <summary>
/// Контекст базы данных для сервиса скоринга
/// </summary>
public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IDomainEventToOutboxConverter outboxConverter,
    ILogger<ApplicationDbContext> logger)
    : DbContext(options), IUnitOfWork
{
    private readonly ILogger<ApplicationDbContext> _logger = logger;
    private IDbContextTransaction? _currentTransaction;

    public DbSet<ScoringRule> ScoringRules => Set<ScoringRule>();
    public DbSet<ScoringRequest> ScoringRequests => Set<ScoringRequest>();
    public DbSet<ScoringResult> ScoringResults => Set<ScoringResult>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<CompensationLog> CompensationLogs => Set<CompensationLog>();
    public DbSet<PendingEnrichedData> PendingEnrichedData => Set<PendingEnrichedData>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    DbSet<T> IUnitOfWork.Set<T>() where T : class => Set<T>();
    EntityEntry<T> IUnitOfWork.Entry<T>(T entity) where T : class => Entry(entity);

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregatesWithEvents = new List<(object aggregate, List<IDomainEvent> events)>();

        foreach (var entry in ChangeTracker.Entries<IAggregateRoot>())
        {
            if (entry.Entity is ScoringRequest request && request.DomainEvents.Any())
            {
                aggregatesWithEvents.Add((request, request.DomainEvents.ToList()));
            }

            if (entry.Entity is CompensationLog compensationLog && compensationLog.DomainEvents.Any())
            {
                aggregatesWithEvents.Add((compensationLog, compensationLog.DomainEvents.ToList()));
            }
        }

        foreach (var (aggregate, events) in aggregatesWithEvents)
        {
            var outboxMessages = outboxConverter.Convert(
                ((dynamic)aggregate).Id.ToString(),
                aggregate.GetType().Name.ToLower(),
                events);

            await OutboxMessages.AddRangeAsync(outboxMessages, cancellationToken);
        }

        foreach (var (aggregate, _) in aggregatesWithEvents)
        {
            ((dynamic)aggregate).ClearDomainEvents();
        }

        var result = await base.SaveChangesAsync(cancellationToken);
        return result;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null) return;
        _currentTransaction = await Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await (_currentTransaction?.CommitAsync(cancellationToken) ?? Task.CompletedTask);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            _currentTransaction?.Dispose();
            _currentTransaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await (_currentTransaction?.RollbackAsync(cancellationToken) ?? Task.CompletedTask);
        }
        finally
        {
            _currentTransaction?.Dispose();
            _currentTransaction = null;
        }
    }
}