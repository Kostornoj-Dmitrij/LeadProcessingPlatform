using Microsoft.EntityFrameworkCore;
using LeadService.Domain.Entities;
using LeadService.Application.Common.Interfaces;
using LeadService.Infrastructure.Data.Entities;
using SharedKernel.Entities;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Storage;
using SharedKernel.Events;
using IUnitOfWork = SharedKernel.Base.IUnitOfWork;
using LeadService.Infrastructure.Outbox;
using Microsoft.Extensions.Logging;

namespace LeadService.Infrastructure.Data;

/// <summary>
/// Контекст базы данных для сервиса лидов
/// </summary>
public class ApplicationDbContext : DbContext, IApplicationDbContext, IUnitOfWork
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
                    "lead",
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
            
            foreach (var entry in ex.Entries)
            {
                if (entry.Entity is Lead lead)
                {
                    var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken);
                    if (databaseValues != null)
                    {
                        var dbVersion = databaseValues.GetValue<uint>("xmin");
                        _logger.LogWarning(
                            "Concurrency conflict for Lead {LeadId}. Entity version: {EntityVersion}, Database version: {DbVersion}",
                            lead.Id, lead.Version, dbVersion);
                    }
                }
            }
            
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