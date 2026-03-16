using IntegrationEvents.Base;

namespace IntegrationEvents.LeadEvents;

/// <summary>
/// Публикуется Lead Service после успешного распределения лида.
/// </summary>
public class LeadDistributedIntegrationEvent : IntegrationEvent
{
    public Guid LeadId { get; set; }

    public string Target { get; set; } = string.Empty;
}