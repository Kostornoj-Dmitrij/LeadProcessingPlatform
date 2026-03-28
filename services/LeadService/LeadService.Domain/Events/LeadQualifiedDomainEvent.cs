using AvroSchemas;
using AvroSchemas.Messages.LeadEvents;
using SharedKernel.Events;

namespace LeadService.Domain.Events;

/// <summary>
/// Доменное событие - лид квалифицирован
/// </summary>
public class LeadQualifiedDomainEvent(
    Guid leadId,
    int score,
    string companyName,
    string? contactPerson,
    string email,
    EnrichedDataDto? enrichedData = null) : DomainEvent
{
    public Guid LeadId { get; } = leadId;
    public int Score { get; } = score;
    public string CompanyName { get; } = companyName;
    public string? ContactPerson { get; } = contactPerson;
    public string Email { get; } = email;
    public EnrichedDataDto? EnrichedData { get; } = enrichedData;

    public override IIntegrationEvent ToIntegrationEvent()
    {
        return new LeadQualified
        {
            EventId = EventId,
            OccurredOnUtc = new DateTimeOffset(OccurredOn).ToUnixTimeMilliseconds(),
            EventType = GetType().Name,
            SchemaVersion = 1,
            LeadId = LeadId,
            CompanyName = CompanyName,
            ContactPerson = ContactPerson,
            Email = Email,
            Score = Score,
            EnrichedData = EnrichedData != null ? AvroSchemas.Messages.LeadEvents.EnrichedData.FromDto(EnrichedData) : null
        };
    }
}