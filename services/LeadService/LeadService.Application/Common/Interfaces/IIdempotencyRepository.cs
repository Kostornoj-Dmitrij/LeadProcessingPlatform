using SharedKernel.Entities;

namespace LeadService.Application.Common.Interfaces;

/// <summary>
/// Репозиторий для атомарной работы с ключами идемпотентности
/// </summary>
public interface IIdempotencyRepository
{
    Task<IdempotencyKey?> TryAcquireLockAsync(string key, byte[] requestHash, TimeSpan lockDuration, CancellationToken cancellationToken);

    Task UpdateWithResultAsync(Guid id, Guid leadId, int responseCode, string responseBody, CancellationToken cancellationToken);

    Task ReleaseLockAsync(Guid id, CancellationToken cancellationToken);

    Task<IdempotencyKey?> GetByKeyAsync(string key, CancellationToken cancellationToken);
}