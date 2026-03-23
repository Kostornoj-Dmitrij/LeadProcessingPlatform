using SharedKernel.Base;

namespace ScoringService.Domain.Entities;

/// <summary>
/// Хранилище обогащенных данных, пришедших до создания запроса на скоринг
/// </summary>
public class PendingEnrichedData : Entity<Guid>
{
    public Guid LeadId { get; private set; }
    public string EnrichedDataJson { get; private set; }
    public DateTime ReceivedAt { get; private set; }
    public bool IsProcessed { get; private set; }

    private PendingEnrichedData(Guid id) : base(id) { }

    public static PendingEnrichedData Create(Guid leadId, string enrichedDataJson)
    {
        return new PendingEnrichedData(Guid.NewGuid())
        {
            LeadId = leadId,
            EnrichedDataJson = enrichedDataJson,
            ReceivedAt = DateTime.UtcNow,
            IsProcessed = false
        };
    }

    public void MarkAsProcessed()
    {
        IsProcessed = true;
    }
}