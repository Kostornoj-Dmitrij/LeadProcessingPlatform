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
    EnrichedDataDto? enrichedData = null)
    : DomainEvent
{
    public Guid LeadId { get; } = leadId;
    public int Score { get; } = score;
    public string CompanyName { get; } = companyName;
    public string? ContactPerson { get; } = contactPerson;
    public string Email { get; } = email;

    public EnrichedDataDto? EnrichedData { get; } = enrichedData;
}