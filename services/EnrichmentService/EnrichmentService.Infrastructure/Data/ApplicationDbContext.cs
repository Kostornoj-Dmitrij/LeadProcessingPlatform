using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using EnrichmentService.Domain.Entities;
using EnrichmentService.Infrastructure.Data.Configurations;
using SharedKernel.Entities;
using SharedKernel.Events;
using EnrichmentService.Infrastructure.Outbox;
using EnrichmentService.Infrastructure.Inbox;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using SharedKernel.Base;

namespace EnrichmentService.Infrastructure.Data;

/// <summary>
/// Контекст базы данных для сервиса обогащения
/// </summary>
public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IDomainEventToOutboxConverter outboxConverter,
    ILogger<ApplicationDbContext> logger)
    : DbContext(options), IUnitOfWork
{
    private readonly ILogger<ApplicationDbContext> _logger = logger;
    private IDbContextTransaction? _currentTransaction;

    public DbSet<EnrichmentResult> EnrichmentResults => Set<EnrichmentResult>();
    public DbSet<EnrichmentRequest> EnrichmentRequests => Set<EnrichmentRequest>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<CompensationLog> CompensationLogs => Set<CompensationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new EnrichmentResultConfiguration());
        modelBuilder.ApplyConfiguration(new EnrichmentRequestConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new CompensationLogConfiguration());
    }

    DbSet<T> IUnitOfWork.Set<T>() where T : class => Set<T>();
    EntityEntry<T> IUnitOfWork.Entry<T>(T entity) where T : class => Entry(entity);

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregatesWithEvents = new List<(object aggregate, List<IDomainEvent> events)>();

        foreach (var entry in ChangeTracker.Entries<IAggregateRoot>())
        {
            if (entry.Entity is EnrichmentResult enrichment && enrichment.DomainEvents.Any())
            {
                aggregatesWithEvents.Add((enrichment, enrichment.DomainEvents.ToList()));
            }
            if (entry.Entity is EnrichmentRequest request && request.DomainEvents.Any())
            {
                aggregatesWithEvents.Add((request, request.DomainEvents.ToList()));
            }
            if (entry.Entity is CompensationLog log)
            {
                if (log.DomainEvents.Any())
                {
                    aggregatesWithEvents.Add((log, log.DomainEvents.ToList()));
                }
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
        if (_currentTransaction != null)
            return;

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