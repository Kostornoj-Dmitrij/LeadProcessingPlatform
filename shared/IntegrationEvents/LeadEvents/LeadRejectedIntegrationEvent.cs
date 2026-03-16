using IntegrationEvents.Base;

namespace IntegrationEvents.LeadEvents;

/// <summary>
/// Публикуется Lead Service при ошибке на этапе обогащения или скоринга.
/// </summary>
public class LeadRejectedIntegrationEvent : IntegrationEvent
{
    public Guid LeadId { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string? ErrorDetails { get; set; }

    public string FailureType { get; set; } = string.Empty;
}