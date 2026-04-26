using System.Text;
using System.Text.Json;
using Npgsql;
using SharedKernel.Json;
using Spectre.Console;

namespace LoadTests.Host.Infrastructure;

/// <summary>
/// Валидатор консистентности обработки лидов
/// </summary>
public class ConsistencyValidator(string connectionString, string apiGatewayUrl)
{
    private readonly HttpClient _httpClient = new();

    public async Task<ValidationReport> ValidateAsync(
        List<Guid> leadIds,
        Dictionary<Guid, ExpectedScenarioPath> expectedPaths,
        CancellationToken cancellationToken = default)
    {
        var report = new ValidationReport();

        if (leadIds.Count == 0)
        {
            report.AddError("No leads to validate");
            return report;
        }

        var history = new Dictionary<Guid, List<LeadStatusHistoryDto>>();
        var leadData = new Dictionary<Guid, LeadConsistencyDto>();

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

        await LoadStatusHistoryInternalAsync(conn, leadIds, history, cancellationToken);
        await LoadLeadDataInternalAsync(conn, leadIds, leadData, cancellationToken);

        report.TotalLeads = leadIds.Count;
        report.LeadsWithHistory = history.Keys.Count;

        foreach (var leadId in leadIds)
        {
            if (!history.TryGetValue(leadId, out var leadHistory) || leadHistory.Count == 0)
            {
                report.AddError(leadId, "No status history found");
                continue;
            }

            var statusSequence = leadHistory.OrderBy(h => h.ChangedAt).ToList();
            var statusNames = new List<string>();

            if (statusSequence[0].OldStatus != null)
                statusNames.Add(statusSequence[0].OldStatus!);

            statusNames.AddRange(statusSequence.Select(s => s.NewStatus));

            if (statusNames[0] != "Initial")
                report.AddError(leadId, $"First status is {statusNames[0]}, expected Initial");

            var finalStatus = statusSequence[^1].NewStatus;
            if (finalStatus != "Closed")
            {
                report.AddError(leadId, $"Final status is {finalStatus}, expected Closed");
                report.StuckLeads.Add(leadId);
            }
            else
            {
                report.CompletedLeads++;
            }

            if (expectedPaths.TryGetValue(leadId, out var expectedPath))
            {
                if (!ValidateExpectedPath(statusNames, expectedPath))
                    report.AddError(leadId, 
                        $"Expected path {GetExpectedSequence(expectedPath)} but got: {string.Join(" -> ", statusNames)}");

                var createdAt = statusSequence[0].ChangedAt;
                var closedAt = statusSequence[^1].ChangedAt;
                var processingTime = (closedAt - createdAt).TotalSeconds;

                report.ProcessingTimes.Add(new LeadProcessingTime
                {
                    LeadId = leadId,
                    ExpectedPath = expectedPath.ToString(),
                    ActualPath = string.Join(" -> ", statusNames),
                    TotalSeconds = processingTime
                });
            }

            if (leadData.TryGetValue(leadId, out var lead))
            {
                if (expectedPaths.TryGetValue(leadId, out expectedPath))
                    ValidateDataConsistency(leadId, lead, expectedPath, report);
            }

            if (!ValidateStateTransitionsFromNames(statusNames))
                report.AddError(leadId, $"Invalid state transition: {string.Join(" -> ", statusNames)}");
        }

        return report;
    }

    private string GetExpectedSequence(ExpectedScenarioPath path)
    {
        return path switch
        {
            ExpectedScenarioPath.Success => "Initial -> Qualified -> Distributed -> Closed",
            ExpectedScenarioPath.EnrichmentFailure => "Initial -> Rejected -> Closed",
            ExpectedScenarioPath.ScoringFailure => "Initial -> Rejected -> Closed",
            ExpectedScenarioPath.DistributionFailure => "Initial -> Qualified -> FailedDistribution -> Closed",
            _ => "Unknown"
        };
    }

    private bool ValidateExpectedPath(List<string> actualStatuses, ExpectedScenarioPath expectedPath)
    {
        string[] expectedSequence = expectedPath switch
        {
            ExpectedScenarioPath.Success => ["Initial", "Qualified", "Distributed", "Closed"],
            ExpectedScenarioPath.EnrichmentFailure => ["Initial", "Rejected", "Closed"],
            ExpectedScenarioPath.ScoringFailure => ["Initial", "Rejected", "Closed"],
            ExpectedScenarioPath.DistributionFailure => ["Initial", "Qualified", "FailedDistribution", "Closed"],
            _ => []
        };

        int expectedIndex = 0;
        foreach (var status in actualStatuses)
        {
            if (expectedIndex < expectedSequence.Length && status == expectedSequence[expectedIndex])
                expectedIndex++;
        }

        return expectedIndex == expectedSequence.Length;
    }

    private async Task LoadStatusHistoryInternalAsync(
        NpgsqlConnection conn,
        List<Guid> leadIds,
        Dictionary<Guid, List<LeadStatusHistoryDto>> result,
        CancellationToken cancellationToken)
    {
        for (int i = 0; i < leadIds.Count; i += 1000)
        {
            var batch = leadIds.Skip(i).Take(1000).ToList();
            var sql = @"
            SELECT lead_id, old_status, new_status, changed_at 
            FROM lead_status_history 
            WHERE lead_id = ANY(@leadIds)
            ORDER BY lead_id, changed_at";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("leadIds", batch);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var leadId = reader.GetGuid(0);
                var record = new LeadStatusHistoryDto
                {
                    LeadId = leadId,
                    OldStatus = reader.IsDBNull(1) ? null : reader.GetString(1),
                    NewStatus = reader.GetString(2),
                    ChangedAt = reader.GetDateTime(3)
                };

                if (!result.ContainsKey(leadId))
                    result[leadId] = [];

                result[leadId].Add(record);
            }
        }
    }

    private async Task LoadLeadDataInternalAsync(
        NpgsqlConnection conn,
        List<Guid> leadIds,
        Dictionary<Guid, LeadConsistencyDto> result,
        CancellationToken cancellationToken)
    {
        for (int i = 0; i < leadIds.Count; i += 1000)
        {
            var batch = leadIds.Skip(i).Take(1000).ToList();
            var sql = @"
            SELECT id, status, score, 
                   is_enrichment_received, is_scoring_received,
                   is_enrichment_compensated, is_scoring_compensated,
                   enriched_data
            FROM leads 
            WHERE id = ANY(@leadIds)";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("leadIds", batch);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                result[reader.GetGuid(0)] = new LeadConsistencyDto
                {
                    Id = reader.GetGuid(0),
                    Status = reader.GetInt32(1).ToString(),
                    Score = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    IsEnrichmentReceived = reader.GetBoolean(3),
                    IsScoringReceived = reader.GetBoolean(4),
                    IsEnrichmentCompensated = reader.GetBoolean(5),
                    IsScoringCompensated = reader.GetBoolean(6),
                    EnrichedData = reader.IsDBNull(7) ? null : reader.GetString(7)
                };
            }
        }
    }

    private void ValidateDataConsistency(
        Guid leadId,
        LeadConsistencyDto lead,
        ExpectedScenarioPath expectedPath,
        ValidationReport report)
    {
        switch (expectedPath)
        {
            case ExpectedScenarioPath.Success:
                if (lead.Score is null or < 0)
                    report.AddError(leadId, $"Success: score should be >= 0, got {lead.Score}");
                if (!lead.IsEnrichmentReceived)
                    report.AddError(leadId, "Success: is_enrichment_received should be true");
                if (!lead.IsScoringReceived)
                    report.AddError(leadId, "Success: is_scoring_received should be true");
                if (lead.IsEnrichmentCompensated)
                    report.AddError(leadId, "Success: is_enrichment_compensated should be false");
                if (lead.IsScoringCompensated)
                    report.AddError(leadId, "Success: is_scoring_compensated should be false");
                if (string.IsNullOrEmpty(lead.EnrichedData))
                    report.AddError(leadId, "Success: enriched_data should not be null");
                break;

            case ExpectedScenarioPath.EnrichmentFailure:
                if (lead.Score != null)
                    report.AddError(leadId, $"EnrichmentFailure: score should be null after compensation, got {lead.Score}");
                if (!lead.IsEnrichmentCompensated)
                    report.AddError(leadId, "EnrichmentFailure: is_enrichment_compensated should be true");
                if (!lead.IsScoringCompensated)
                    report.AddError(leadId, "EnrichmentFailure: is_scoring_compensated should be true");
                if (!string.IsNullOrEmpty(lead.EnrichedData))
                    report.AddError(leadId, "EnrichmentFailure: enriched_data should be null after compensation");
                break;

            case ExpectedScenarioPath.ScoringFailure:
                if (lead.Score != null)
                    report.AddError(leadId, $"ScoringFailure: score should be null after compensation, got {lead.Score}");
                if (!lead.IsEnrichmentCompensated)
                    report.AddError(leadId, "ScoringFailure: is_enrichment_compensated should be true");
                if (!lead.IsScoringCompensated)
                    report.AddError(leadId, "ScoringFailure: is_scoring_compensated should be true");
                if (!string.IsNullOrEmpty(lead.EnrichedData))
                    report.AddError(leadId, "ScoringFailure: enriched_data should be null after compensation");
                break;

            case ExpectedScenarioPath.DistributionFailure:
                if (lead.Score != null)
                    report.AddError(leadId, $"DistributionFailure: score should be null after compensation, got {lead.Score}");
                if (!lead.IsEnrichmentCompensated)
                    report.AddError(leadId, "DistributionFailure: is_enrichment_compensated should be true");
                if (!lead.IsScoringCompensated)
                    report.AddError(leadId, "DistributionFailure: is_scoring_compensated should be true");
                if (!string.IsNullOrEmpty(lead.EnrichedData))
                    report.AddError(leadId, "DistributionFailure: enriched_data should be null after compensation");
                break;
        }
    }

    private bool ValidateStateTransitionsFromNames(List<string> statuses)
    {
        var validTransitions = new Dictionary<string, HashSet<string>>
        {
            ["Initial"] = ["Qualified", "Rejected"],
            ["Qualified"] = ["Distributed", "FailedDistribution"],
            ["Distributed"] = ["Closed"],
            ["FailedDistribution"] = ["Closed"],
            ["Rejected"] = ["Closed"],
            ["Closed"] = []
        };

        for (int i = 0; i < statuses.Count - 1; i++)
        {
            var current = statuses[i];
            var next = statuses[i + 1];

            if (!validTransitions.TryGetValue(current, out var allowed) || !allowed.Contains(next))
                return false;
        }

        return true;
    }

    public async Task<bool> WaitForAllClosedAsync(
        List<Guid> leadIds,
        int maxWaitSeconds = 300,
        CancellationToken cancellationToken = default)
    {
        var startedAt = DateTime.UtcNow;
        var checkInterval = TimeSpan.FromSeconds(2);
        var allCompleted = false;
        var token = await AuthHelper.GetTokenAsync(apiGatewayUrl);

        await AnsiConsole.Live(new Table().AddColumn("Status"))
            .StartAsync(async ctx =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var elapsed = (DateTime.UtcNow - startedAt).TotalSeconds;
                    if (elapsed > maxWaitSeconds)
                    {
                        ctx.UpdateTarget(new Markup("[red]Timeout waiting for completion[/]"));
                        allCompleted = false;
                        break;
                    }

                    var content = new StringContent(
                        JsonSerializer.Serialize(leadIds, JsonDefaults.Options),
                        Encoding.UTF8,
                        "application/json");

                    using var request = new HttpRequestMessage(HttpMethod.Post, $"{apiGatewayUrl}/api/leads/status-summary");
                    request.Content = content;
                    request.Headers.Add("Authorization", $"Bearer {token}");

                    var response = await _httpClient.SendAsync(request, cancellationToken);

                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        token = await AuthHelper.GetTokenAsync(apiGatewayUrl);
                        continue;
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync(cancellationToken);
                        var summary = JsonSerializer.Deserialize<StatusSummaryResponse>(json, JsonDefaults.Options);

                        var table = new Table()
                            .AddColumn("Metric")
                            .AddColumn("Value");
                        table.AddRow("Total", summary!.Total.ToString());
                        table.AddRow("Closed", $"[green]{summary.Closed}[/]");
                        table.AddRow("Rejected", $"[yellow]{summary.Rejected}[/]");
                        table.AddRow("FailedDistribution", $"[orange3]{summary.FailedDistribution}[/]");
                        table.AddRow("In Progress", $"[blue]{summary.InProgress}[/]");
                        table.AddRow("Elapsed", $"{elapsed:F0}s / {maxWaitSeconds}s");

                        ctx.UpdateTarget(table);

                        if (summary.AllCompleted)
                        {
                            ctx.UpdateTarget(new Markup("[green]✓ All leads completed![/]"));
                            allCompleted = true;
                            break;
                        }
                    }

                    await Task.Delay(checkInterval, cancellationToken);
                }
            });

        return allCompleted;
    }
}