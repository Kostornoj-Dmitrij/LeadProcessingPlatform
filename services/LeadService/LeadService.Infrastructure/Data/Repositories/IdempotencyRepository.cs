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
    public async Task<IdempotencyKey?> TryAcquireLockAsync(
        string key, 
        byte[] requestHash, 
        TimeSpan lockDuration, 
        CancellationToken cancellationToken)
    {
        return await TryAcquireLockInternalAsync(key, requestHash, lockDuration, cancellationToken);
    }

    private async Task<IdempotencyKey?> TryAcquireLockInternalAsync(
        string key, 
        byte[] requestHash, 
        TimeSpan lockDuration,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var lockedUntil = now.Add(lockDuration);

        var sql = @"
            INSERT INTO idempotency_keys (id, key, request_hash, created_at, locked_until)
            VALUES (gen_random_uuid(), @key, @requestHash, @now, @lockedUntil)
            ON CONFLICT (key) DO UPDATE SET
                locked_until = EXCLUDED.locked_until
            WHERE idempotency_keys.locked_until IS NULL 
               OR idempotency_keys.locked_until < @now
            RETURNING *;";

        var lockedKeys = await context.IdempotencyKeys
            .FromSqlRaw(sql,
                new NpgsqlParameter("@key", key),
                new NpgsqlParameter("@requestHash", requestHash),
                new NpgsqlParameter("@now", now),
                new NpgsqlParameter("@lockedUntil", lockedUntil))
            .ToListAsync(cancellationToken);

        var lockedKey = lockedKeys.FirstOrDefault();
        if (lockedKey == null)
        {
            return null;
        }
        if (!lockedKey.RequestHash.SequenceEqual(requestHash))
        {
            await context.Database.ExecuteSqlRawAsync(
                "UPDATE idempotency_keys SET locked_until = NULL WHERE id = @id",
                new NpgsqlParameter("@id", lockedKey.Id),
                cancellationToken);

            throw new InvalidOperationException("Idempotency key used with different request data");
        }
        return await context.IdempotencyKeys.FindAsync([lockedKey.Id], cancellationToken);
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