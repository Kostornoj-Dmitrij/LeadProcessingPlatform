using Microsoft.EntityFrameworkCore;
using LeadService.Application.Common.Interfaces;
using SharedKernel.Entities;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace LeadService.Infrastructure.Data.Repositories;

/// <summary>
/// Репозиторий для атомарной работы с ключами идемпотентности
/// </summary>
public class IdempotencyRepository(
    ApplicationDbContext context,
    ILogger<IdempotencyRepository> logger)
    : IIdempotencyRepository
{
    private const int MaxRetryAttempts = 3;

    public async Task<IdempotencyKey?> TryAcquireLockAsync(
        string key, 
        byte[] requestHash, 
        TimeSpan lockDuration, 
        CancellationToken cancellationToken)
    {
        return await TryAcquireLockInternalAsync(key, requestHash, lockDuration, 0, cancellationToken);
    }

    private async Task<IdempotencyKey?> TryAcquireLockInternalAsync(
        string key, 
        byte[] requestHash, 
        TimeSpan lockDuration, 
        int attempt,
        CancellationToken cancellationToken)
    {
        if (attempt >= MaxRetryAttempts)
        {
            logger.LogWarning("Max retry attempts ({MaxAttempts}) reached for key {Key}", MaxRetryAttempts, key);
            throw new InvalidOperationException($"Unable to acquire lock for key {key} after {MaxRetryAttempts} attempts");
        }

        var now = DateTime.UtcNow;
        var lockedUntil = now.Add(lockDuration);
        
        var existing = await context.IdempotencyKeys
            .FromSqlRaw(@"
                SELECT * FROM ""IdempotencyKeys"" 
                WHERE ""IdempotencyKey"" = {0} 
                AND (""LockedUntil"" IS NULL OR ""LockedUntil"" < {1})
                LIMIT 1
                FOR UPDATE SKIP LOCKED",
                key, now)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing != null)
        {
            if (!existing.RequestHash.SequenceEqual(requestHash))
            {
                throw new InvalidOperationException("Idempotency key used with different request data");
            }

            existing.LockedUntil = lockedUntil;
            await context.SaveChangesAsync(cancellationToken);
            
            logger.LogDebug("Acquired lock for existing idempotency key {Key}", key);
            return existing;
        }

        try
        {
            var newKey = new IdempotencyKey
            {
                Id = Guid.NewGuid(),
                Key = key,
                RequestHash = requestHash,
                CreatedAt = now,
                LockedUntil = lockedUntil
            };

            await context.IdempotencyKeys.AddAsync(newKey, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            
            logger.LogDebug("Created and locked new idempotency key {Key}", key);
            return newKey;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            logger.LogDebug("Race condition detected for key {Key}, attempt {Attempt}", key, attempt + 1);
            
            await Task.Delay(Random.Shared.Next(50, 150), cancellationToken);
            
            return await TryAcquireLockInternalAsync(key, requestHash, lockDuration, attempt + 1, cancellationToken);
        }
    }

    public async Task UpdateWithResultAsync(
        Guid id, 
        Guid leadId, 
        int responseCode, 
        string responseBody, 
        CancellationToken cancellationToken)
    {
        var key = await context.IdempotencyKeys.FindAsync([id], cancellationToken);
        if (key != null)
        {
            key.LeadId = leadId;
            key.ResponseCode = responseCode;
            key.ResponseBody = responseBody;
            key.LockedUntil = null;
            
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task ReleaseLockAsync(Guid id, CancellationToken cancellationToken)
    {
        var key = await context.IdempotencyKeys.FindAsync([id], cancellationToken);
        if (key != null)
        {
            key.LockedUntil = null;
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IdempotencyKey?> GetByKeyAsync(string key, CancellationToken cancellationToken)
    {
        return await context.IdempotencyKeys
            .FirstOrDefaultAsync(x => x.Key == key, cancellationToken);
    }
}