using SharedKernel.Events;

namespace LeadService.Domain.Events;

/// <summary>
/// Доменное событие - создание нового лида.
/// </summary>
public sealed class LeadCreatedDomainEvent(
    Guid leadId,
    string source,
    string companyName,
    string? contactPerson,
    string email,
    string? phone,
    string? externalLeadId,
    Dictionary<string, string>? customFields = null)
    : DomainEvent
{
    public Guid LeadId { get; } = leadId;
    public string Source { get; } = source;
    public string CompanyName { get; } = companyName;
    public string? ContactPerson { get; } = contactPerson;
    public string Email { get; } = email;
    public string? Phone { get; } = phone;
    public string? ExternalLeadId { get; } = externalLeadId;
    public Dictionary<string, string>? CustomFields { get; } = customFields;
}