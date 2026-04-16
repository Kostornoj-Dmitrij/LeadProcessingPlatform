using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedHosting.Constants;

namespace SharedInfrastructure.Inbox;

/// <summary>
/// Реализация Inbox хранилища через EF Core
/// </summary>
public class InboxStore<TContext>(
    TContext context,
    ILogger<InboxStore<TContext>> logger)
    : IInboxStore where TContext : DbContext
{
    private const int MaxRetryAttempts = 5;

    public async Task<bool> TryAddAsync(
        string messageId,
        string topic,
        string key,
        string eventType,
        string payload,
        string? traceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new InboxMessage
            {
                Id = Guid.NewGuid(),
                MessageId = messageId,
                Topic = topic,
                Key = key,
                EventType = eventType,
                Payload = payload,
                TraceId = traceId,
                ReceivedAt = DateTime.UtcNow,
                ProcessingAttempts = 0
            };

            await context.Set<InboxMessage>().AddAsync(message, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogDebug("Message {MessageId} added to inbox", messageId);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: ConfigurationKeys.PostgresUniqueViolationSqlState })
        {
            logger.LogDebug("Message {MessageId} was already added by another process", messageId);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding message {MessageId} to inbox", messageId);
            throw;
        }
    }

    public async Task<List<InboxMessage>> GetPendingMessagesAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var messages = await context.Set<InboxMessage>()
            .Where(x => x.ProcessedAt == null)
            .Where(x => x.NextRetryAt == null || x.NextRetryAt <= now)
            .Where(x => x.ProcessingAttempts < MaxRetryAttempts)
            .OrderBy(x => x.ReceivedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        return messages;
    }

    public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await context.Set<InboxMessage>().FindAsync([messageId], cancellationToken);
        if (message != null)
        {
            message.ProcessedAt = DateTime.UtcNow;
            message.ErrorMessage = null;
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task IncrementAttemptsAsync(
        Guid messageId,
        string errorMessage,
        DateTime? nextRetryAt,
        CancellationToken cancellationToken = default)
    {
        var message = await context.Set<InboxMessage>().FindAsync([messageId], cancellationToken);
        if (message != null)
        {
            message.ProcessingAttempts++;
            message.ErrorMessage = errorMessage;
            message.NextRetryAt = nextRetryAt;

            if (message.ProcessingAttempts >= MaxRetryAttempts)
            {
                message.ProcessedAt = DateTime.UtcNow;
                logger.LogWarning("Message {MessageId} reached max attempts ({Attempts}). Marking as failed.",
                    message.MessageId, message.ProcessingAttempts);
            }

            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<InboxMessage?> GetByMessageIdAsync(
        string messageId,
        CancellationToken cancellationToken = default)
    {
        return await context.Set<InboxMessage>()
            .FirstOrDefaultAsync(x => x.MessageId == messageId, cancellationToken);
    }

    public async Task MoveToDeadLetterQueueAsync(
        Guid messageId,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        var message = await context.Set<InboxMessage>().FindAsync([messageId], cancellationToken);
        if (message != null)
        {
            message.ProcessedAt = DateTime.UtcNow;
            message.ErrorMessage = $"MOVED TO DLQ: {errorMessage}";
            message.ProcessingAttempts++;

            await context.SaveChangesAsync(cancellationToken);

            logger.LogWarning(
                "Message {MessageId} moved to DLQ after {Attempts} attempts. Error: {Error}",
                message.MessageId,
                message.ProcessingAttempts,
                errorMessage);
        }
    }
}