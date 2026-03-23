using LeadService.Domain.Enums;
using LeadService.Domain.Events;
using LeadService.Domain.ValueObjects;
using SharedKernel.Base;
using SharedKernel.Events;
using System.Text.Json;
using SharedKernel.Json;

namespace LeadService.Domain.Entities;

/// <summary>
/// Агрегат Lead
/// </summary>
public sealed class Lead : Entity<Guid>, IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = new();
    private readonly List<LeadCustomField> _customFields = new();

    private Lead(Guid id) : base(id)
    {
    }

    private Lead() { }

    public string? ExternalLeadId { get; private set; }

    public string Source { get; private set; } = string.Empty;

    public CompanyName CompanyName { get; private set; } = null!;

    public string? ContactPerson { get; private set; }

    public Email Email { get; private set; } = null!;

    public Phone? Phone { get; private set; }

    public LeadStatus Status { get; private set; }

    public int? Score { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public bool IsEnrichmentReceived { get; private set; }

    public bool IsScoringReceived { get; private set; }

    public string? EnrichedData { get; private set; }

    public bool IsEnrichmentCompensated { get; private set; }

    public bool IsScoringCompensated { get; private set; }

    public IReadOnlyCollection<LeadCustomField> CustomFields => _customFields.AsReadOnly();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public static Lead Create(
        Guid id,
        string source,
        string companyName,
        string email,
        string? externalLeadId = null,
        string? contactPerson = null,
        string? phone = null,
        Dictionary<string, string>? customFields = null)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Lead ID cannot be empty.", nameof(id));

        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source cannot be empty.", nameof(source));

        var lead = new Lead(id)
        {
            ExternalLeadId = externalLeadId,
            Source = source,
            CompanyName = CompanyName.Create(companyName),
            ContactPerson = contactPerson,
            Email = Email.Create(email),
            Phone = phone != null ? Phone.Create(phone) : null,
            Status = LeadStatus.Initial,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,

            IsEnrichmentReceived = false,
            IsScoringReceived = false,
            IsEnrichmentCompensated = false,
            IsScoringCompensated = false
        };

        if (customFields != null)
        {
            foreach (var field in customFields)
            {
                lead._customFields.Add(new LeadCustomField(field.Key, field.Value));
            }
        }

        lead.AddDomainEvent(new LeadCreatedDomainEvent(
            lead.Id,
            lead.Source,
            lead.CompanyName.Value,
            lead.ContactPerson,
            lead.Email.Value,
            lead.Phone?.Value,
            lead.ExternalLeadId,
            customFields));

        return lead;
    }

    public void MarkEnrichmentReceived(string enrichedDataJson)
    {
        if (Status != LeadStatus.Initial)
            throw new InvalidOperationException($"Cannot receive enrichment in status {Status}");

        if (IsEnrichmentReceived)
            return;

        IsEnrichmentReceived = true;
        EnrichedData = enrichedDataJson;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new EnrichmentReceivedDomainEvent(Id));

        TryQualify();
    }

    public void MarkScoringReceived(int score)
    {
        if (Status != LeadStatus.Initial)
            throw new InvalidOperationException($"Cannot receive scoring in status {Status}");

        if (IsScoringReceived)
            return;

        IsScoringReceived = true;
        Score = score;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ScoringReceivedDomainEvent(Id));

        TryQualify();
    }

    private void TryQualify()
    {
        if (Status != LeadStatus.Initial || !IsEnrichmentReceived || !IsScoringReceived)
            return;

        Status = LeadStatus.Qualified;
        UpdatedAt = DateTime.UtcNow;

        EnrichedDataDto? enrichedData = null;
        if (!string.IsNullOrEmpty(EnrichedData))
        {
            try
            {
                enrichedData = JsonSerializer.Deserialize<EnrichedDataDto>(EnrichedData, JsonDefaults.Options);

                if (enrichedData == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to deserialize EnrichedData for lead {Id}: result is null");
                }
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"Failed to deserialize EnrichedData for lead {Id}: {ex.Message}", ex);
            }
        }

        AddDomainEvent(new LeadQualifiedDomainEvent(
            Id, 
            Score!.Value, 
            CompanyName.Value, 
            ContactPerson,
            Email.Value,
            enrichedData));
    }

    public void Reject(string reason, string failureType)
    {
        if (Status == LeadStatus.Rejected)
            return;
        if (Status != LeadStatus.Initial)
            throw new InvalidOperationException($"Cannot reject lead from status {Status}");

        Status = LeadStatus.Rejected;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new LeadRejectedDomainEvent(Id, reason, failureType));
    }

    public void MarkAsDistributed(string target)
    {
        if (Status == LeadStatus.Distributed)
            return;
        if (Status != LeadStatus.Qualified)
            throw new InvalidOperationException($"Cannot distribute lead from status {Status}");

        Status = LeadStatus.Distributed;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new LeadDistributedDomainEvent(Id, target));
    }

    public void MarkDistributionFailed(string reason)
    {
        if (Status == LeadStatus.FailedDistribution)
            return;
        if (Status != LeadStatus.Qualified)
            throw new InvalidOperationException($"Cannot fail distribution from status {Status}");

        Status = LeadStatus.FailedDistribution;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new LeadDistributionFailedDomainEvent(Id, reason));
    }

    public void MarkEnrichmentCompensated()
    {
        if (Status != LeadStatus.Rejected && Status != LeadStatus.FailedDistribution)
            throw new InvalidOperationException($"Cannot receive enrichment compensation in status {Status}");

        if (IsEnrichmentCompensated)
            return;

        IsEnrichmentCompensated = true;
        EnrichedData = null;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new EnrichmentCompensatedDomainEvent(Id));

        TryCloseAfterCompensation();
    }

    public void MarkScoringCompensated()
    {
        if (Status != LeadStatus.Rejected && Status != LeadStatus.FailedDistribution)
            throw new InvalidOperationException($"Cannot receive scoring compensation in status {Status}");

        if (IsScoringCompensated)
            return;

        IsScoringCompensated = true;
        Score = null;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ScoringCompensatedDomainEvent(Id));

        TryCloseAfterCompensation();
    }

    private void TryCloseAfterCompensation()
    {
        if ((Status != LeadStatus.Rejected && Status != LeadStatus.FailedDistribution) ||
            !IsEnrichmentCompensated || !IsScoringCompensated)
            return;

        var previousStatus = Status;

        Status = LeadStatus.Closed;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new LeadClosedDomainEvent(Id, previousStatus));
    }

    public void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    private void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public void CloseAfterDistribution()
    {
        if (Status != LeadStatus.Distributed)
            throw new InvalidOperationException($"Cannot close lead from status {Status}");

        var previousStatus = Status;
        Status = LeadStatus.Closed;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new LeadClosedDomainEvent(Id, previousStatus));
    }
}