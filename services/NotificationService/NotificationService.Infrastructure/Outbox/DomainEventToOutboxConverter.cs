using Microsoft.Extensions.Logging;
using SharedInfrastructure.Outbox;

namespace NotificationService.Infrastructure.Outbox;

/// <summary>
/// Конвертер доменных событий в outbox-сообщения
/// </summary>
public class DomainEventToOutboxConverter(ILogger<DomainEventToOutboxConverter> logger) 
    : BaseDomainEventToOutboxConverter(logger);