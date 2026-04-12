using System.Collections.Concurrent;

namespace LoadTests.Host.Infrastructure;

/// <summary>
/// Генератор и трекер созданных лидов
/// </summary>
public class LeadGenerator
{
    private readonly ConcurrentBag<Guid> _createdLeadIds = new();
    private readonly ConcurrentDictionary<Guid, ExpectedScenarioPath> _expectedPaths = new();

    public IReadOnlyCollection<Guid> CreatedLeadIds => _createdLeadIds;
    public IReadOnlyDictionary<Guid, ExpectedScenarioPath> ExpectedPaths => _expectedPaths;

    public void TrackCreatedLead(Guid leadId, ExpectedScenarioPath path)
    {
        _createdLeadIds.Add(leadId);
        _expectedPaths[leadId] = path;
    }

    public void Clear()
    {
        _createdLeadIds.Clear();
        _expectedPaths.Clear();
    }
}