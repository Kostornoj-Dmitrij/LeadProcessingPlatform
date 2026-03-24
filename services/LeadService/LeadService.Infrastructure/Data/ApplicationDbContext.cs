using Microsoft.EntityFrameworkCore;
using LeadService.Domain.Entities;
using LeadService.Infrastructure.Data.Entities;
using SharedKernel.Entities;
using System.Reflection;
using LeadService.Domain.Enums;
using Microsoft.EntityFrameworkCore.Storage;
using SharedKernel.Events;
using IUnitOfWork = SharedKernel.Base.IUnitOfWork;
using LeadService.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using LeadService.Domain.Constants;

namespace LeadService.Infrastructure.Data;

/// <summary>
/// Контекст базы данных для сервиса лидов
/// </summary>
public class ApplicationDbContext : DbContext, IUnitOfWork
{
    private readonly IDomainEventToOutboxConverter _outboxConverter;
    private readonly ILogger<ApplicationDbContext> _logger;
    private IDbContextTransaction? _currentTransaction;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IDomainEventToOutboxConverter outboxConverter,
        ILogger<ApplicationDbContext> logger)
        : base(options)
    {
        _outboxConverter = outboxConverter;
        _logger = logger;
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
        : base(options) 
    {
        _outboxConverter = null!;
        _logger = null!;
    }

    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<LeadCustomField> LeadCustomFields => Set<LeadCustomField>();
    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<LeadStatusHistory> LeadStatusHistories => Set<LeadStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    DbSet<T> IUnitOfWork.Set<T>() where T : class => Set<T>();
    EntityEntry<T> IUnitOfWork.Entry<T>(T entity) where T : class => Entry(entity);

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            foreach (var entry in ChangeTracker.Entries<Lead>())
            {
                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdateTimestamp();
                }
            }

            var statusHistories = new List<LeadStatusHistory>();

            foreach (var entry in ChangeTracker.Entries<Lead>())
            {
                if (entry.State == EntityState.Modified)
                {
                    var originalStatus = (LeadStatus?)entry.OriginalValues["Status"];
                    var currentStatus = entry.Entity.Status;

                    if (originalStatus != currentStatus)
                    {
                        statusHistories.Add(new LeadStatusHistory
                        {
                            Id = Guid.NewGuid(),
                            LeadId = entry.Entity.Id,
                            OldStatus = originalStatus?.ToString(),
                            NewStatus = currentStatus.ToString(),
                            ChangedAt = DateTime.UtcNow,
                            EventId = entry.Entity.DomainEvents.LastOrDefault()?.EventId
                        });
                    }
                }
            }

            if (statusHistories.Any())
            {
                await LeadStatusHistories.AddRangeAsync(statusHistories, cancellationToken);
            }
            var aggregatesWithEvents = new List<(Lead aggregate, List<IDomainEvent> events)>();

            foreach (var entry in ChangeTracker.Entries<Lead>())
            {
                var events = entry.Entity.DomainEvents.ToList();
                if (events.Any())
                {
                    aggregatesWithEvents.Add((entry.Entity, events));
                }
            }

            foreach (var (aggregate, events) in aggregatesWithEvents)
            {
                var outboxMessages = _outboxConverter.Convert(
                    aggregate.Id.ToString(),
                    AggregateConstants.Lead,
                    events);

                await OutboxMessages.AddRangeAsync(outboxMessages, cancellationToken);
            }

            foreach (var entry in ChangeTracker.Entries<Lead>())
            {
                entry.Entity.ClearDomainEvents();
            }

            var result = await base.SaveChangesAsync(cancellationToken);

            return result;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict detected while saving changes for lead");
            throw;
        }
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