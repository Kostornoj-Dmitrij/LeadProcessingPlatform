using System.Diagnostics;
using System.Text.Json;
using AvroSchemas.Messages.LeadEvents;
using DistributionService.Application.Metrics;
using DistributionService.Domain.Entities;
using DistributionService.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedInfrastructure.Telemetry;
using SharedKernel.Base;
using SharedKernel.Json;

namespace DistributionService.Application.EventHandlers;

/// <summary>
/// Обработчик события LeadQualified
/// </summary>
public class LeadQualifiedEventHandler(
    IUnitOfWork unitOfWork,
    ILogger<LeadQualifiedEventHandler> logger)
    : INotificationHandler<LeadQualified>
{
    public async Task Handle(LeadQualified @event, CancellationToken cancellationToken)
    {
        using var activity = ActivityBuilderExtensions.CreateEventActivity(@event)
            .WithTags(
                (TelemetryAttributes.LeadScore, @event.Score),
                (TelemetryAttributes.LeadCompany, @event.CompanyName),
                (TelemetryAttributes.LeadEmail, @event.Email))
            .WithProcessingStep("distribution_request_creation");

        logger.LogInformation("Processing LeadQualified for lead {LeadId} with score {Score}",
            @event.LeadId, @event.Score);

        var existingRequest = await unitOfWork.Set<DistributionRequest>()
            .FirstOrDefaultAsync(x => x.LeadId == @event.LeadId, cancellationToken);

        if (existingRequest != null)
        {
            logger.LogInformation("Lead {LeadId} already has a distribution request in status {Status}, skipping",
                @event.LeadId, existingRequest.Status);
            return;
        }

        var rules = await unitOfWork.Set<DistributionRule>()
            .Where(r => r.IsActive && (r.ValidTo == null || r.ValidTo > DateTime.UtcNow))
            .OrderBy(r => r.Priority)
            .ToListAsync(cancellationToken);

        if (!rules.Any())
        {
            logger.LogWarning("No active distribution rules found for lead {LeadId}", @event.LeadId);
            await CreateFailedHistoryRecord(@event, null, "No active distribution rules found", cancellationToken);
            return;
        }

        DistributionRule? applicableRule = null;

        foreach (var rule in rules)
        {
            if (IsRuleApplicable(rule, @event))
            {
                applicableRule = rule;
                break;
            }
        }

        if (applicableRule == null)
        {
            logger.LogWarning("No applicable distribution rule found for lead {LeadId}", @event.LeadId);
            await CreateFailedHistoryRecord(@event, null, "No applicable distribution rule found", cancellationToken);
            return;
        }

        var target = ResolveTarget(applicableRule, @event);

        if (string.IsNullOrEmpty(target))
        {
            logger.LogWarning("Failed to resolve target for lead {LeadId} using rule {RuleName}",
                @event.LeadId, applicableRule.RuleName);
            await CreateFailedHistoryRecord(@event, applicableRule.Id, "Failed to resolve distribution target", cancellationToken);
            return;
        }

        var traceParent = TelemetryContext.GetTraceParent();

        string? enrichedDataJson = null;
        if (@event.EnrichedData != null)
        {
            enrichedDataJson = JsonSerializer.Serialize(@event.EnrichedData, JsonDefaults.Options);
        }

        var request = DistributionRequest.Create(
            leadId: @event.LeadId,
            companyName: @event.CompanyName,
            email: @event.Email,
            score: @event.Score,
            contactPerson: @event.ContactPerson,
            customFields: @event.CustomFields,
            enrichedData: enrichedDataJson,
            traceParent: traceParent);

        request.SetRuleAndTarget(applicableRule.Id, target);

        await unitOfWork.Set<DistributionRequest>().AddAsync(request, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        DistributionMetrics.DistributionRequests.Add(1, new TagList { { "status", "pending" } });
        logger.LogInformation(
            "Distribution request created for lead {LeadId} with target {Target} using rule {RuleName}",
            @event.LeadId, target, applicableRule.RuleName);
    }

    private bool IsRuleApplicable(DistributionRule rule, LeadQualified @event)
    {
        try
        {
            var condition = JsonSerializer.Deserialize<Dictionary<string, object>>(rule.ConditionJson, JsonDefaults.Options);
            if (condition == null || !condition.TryGetValue("type", out var typeObj))
                return false;

            var type = typeObj.ToString();

            return type switch
            {
                "score_threshold" => EvaluateScoreThreshold(condition, @event.Score),
                "industry_match" => EvaluateIndustryMatch(condition, @event.EnrichedData?.Industry),
                "revenue_range" => EvaluateRevenueRange(condition, @event.EnrichedData?.RevenueRange),
                "always_true" => true,
                _ => false
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error evaluating rule {RuleName} for lead {LeadId}",
                rule.RuleName, @event.LeadId);
            return false;
        }
    }

    private bool EvaluateScoreThreshold(Dictionary<string, object> condition, int score)
    {
        if (!condition.TryGetValue("min_score", out var minScoreObj))
            return true;

        return int.TryParse(minScoreObj.ToString(), out var minScore) && score >= minScore;
    }

    private bool EvaluateIndustryMatch(Dictionary<string, object> condition, string? industry)
    {
        if (string.IsNullOrEmpty(industry) || !condition.TryGetValue("industry", out var industryObj))
            return false;
        return industry.Equals(industryObj.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private bool EvaluateRevenueRange(Dictionary<string, object> condition, string? revenueRange)
    {
        if (string.IsNullOrEmpty(revenueRange) || !condition.TryGetValue("range", out var rangeObj))
            return false;
        return revenueRange.Equals(rangeObj.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private string ResolveTarget(DistributionRule rule, LeadQualified @event)
    {
        try
        {
            var config = JsonSerializer.Deserialize<Dictionary<string, object>>(rule.TargetConfigJson, JsonDefaults.Options);
            if (config == null)
                return string.Empty;

            return rule.Strategy switch
            {
                DistributionRuleStrategy.FixedTarget => config.GetValueOrDefault("target")?.ToString() ?? string.Empty,
                DistributionRuleStrategy.ScoreBased => ResolveScoreBasedTarget(config, @event.Score),
                DistributionRuleStrategy.RoundRobin => ResolveRoundRobinTarget(config, rule.Id),
                DistributionRuleStrategy.Territory => ResolveTerritoryTarget(config, @event),
                DistributionRuleStrategy.Specialization => ResolveSpecializationTarget(config, @event),
                _ => config.GetValueOrDefault("target")?.ToString() ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resolving target for rule {RuleName}", rule.RuleName);
            return string.Empty;
        }
    }

    private string ResolveTerritoryTarget(Dictionary<string, object> config, LeadQualified @event)
    {
        if (!config.TryGetValue("territories", out var territoriesObj))
            return string.Empty;

        Dictionary<string, string> territories;

        if (territoriesObj is JsonElement jsonElement)
        {
            territories = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonElement.GetRawText(), JsonDefaults.Options)
                          ?? new();
        }
        else if (territoriesObj is Dictionary<string, object> dict)
        {
            territories = dict.ToDictionary(k => k.Key, v => v.Value.ToString() ?? string.Empty);
        }
        else
        {
            return string.Empty;
        }

        var territory = @event.EnrichedData?.Industry.ToLower() ?? "default";
        return territories.GetValueOrDefault(territory) ?? territories.GetValueOrDefault("default") ?? string.Empty;
    }

    private string ResolveSpecializationTarget(Dictionary<string, object> config, LeadQualified @event)
    {
        if (!config.TryGetValue("specializations", out var specializationsObj))
            return string.Empty;

        Dictionary<string, string> specializations;

        if (specializationsObj is JsonElement jsonElement)
        {
            specializations = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonElement.GetRawText(), JsonDefaults.Options)
                              ?? new();
        }
        else if (specializationsObj is Dictionary<string, object> dict)
        {
            specializations = dict.ToDictionary(k => k.Key, v => v.Value.ToString() ?? string.Empty);
        }
        else
        {
            return string.Empty;
        }

        var specialization = @event.EnrichedData?.CompanySize.ToLower() ?? "default";
        return specializations.GetValueOrDefault(specialization) ?? specializations.GetValueOrDefault("default") ?? string.Empty;
    }

    private string ResolveScoreBasedTarget(Dictionary<string, object> config, int score)
    {
        if (!config.TryGetValue("thresholds", out var thresholdsObj))
            return config.GetValueOrDefault("default_target")?.ToString() ?? string.Empty;

        List<Dictionary<string, object>> thresholds;

        try
        {
            if (thresholdsObj is JsonElement jsonElement)
            {
                thresholds = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonElement.GetRawText(), JsonDefaults.Options) ??
                             [];
            }
            else if (thresholdsObj is List<object> list)
            {
                thresholds = list.Cast<Dictionary<string, object>>().ToList();
            }
            else
            {
                logger.LogWarning("Invalid thresholds type in ScoreBased config: {ThresholdsType}. Using default_target.", 
                    thresholdsObj.GetType().Name);
                return config.GetValueOrDefault("default_target")?.ToString() ?? string.Empty;
            }
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to deserialize thresholds in ScoreBased config. Using default_target.");
            return config.GetValueOrDefault("default_target")?.ToString() ?? string.Empty;
        }

        foreach (var threshold in thresholds.OrderByDescending(t =>
                     GetInt32(t.GetValueOrDefault("min_score"))))
        {
            if (score >= GetInt32(threshold.GetValueOrDefault("min_score")))
            {
                var target = threshold.GetValueOrDefault("target")?.ToString();
                if (!string.IsNullOrEmpty(target))
                    return target;
            }
        }

        return config.GetValueOrDefault("default_target")?.ToString() ?? string.Empty;
    }

    private string ResolveRoundRobinTarget(Dictionary<string, object> config, Guid ruleId)
    {
        if (!config.TryGetValue("targets", out var targetsObj))
            return string.Empty;

        List<string> targets;

        if (targetsObj is JsonElement jsonElement)
        {
            targets = JsonSerializer.Deserialize<List<string>>(jsonElement.GetRawText(), JsonDefaults.Options) ?? new();
        }
        else if (targetsObj is List<object> list)
        {
            targets = list.Select(x => x.ToString() ?? string.Empty).ToList();
        }
        else
        {
            return string.Empty;
        }

        if (!targets.Any())
            return string.Empty;

        var index = Math.Abs(ruleId.GetHashCode()) % targets.Count;
        return targets[index];
    }

    private async Task CreateFailedHistoryRecord(
        LeadQualified @event,
        Guid? ruleId,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        var history = DistributionHistory.CreateFailed(
            @event.LeadId,
            ruleId,
            errorMessage);

        await unitOfWork.Set<DistributionHistory>().AddAsync(history, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static int GetInt32(object? value)
    {
        return value switch
        {
            JsonElement jsonElement => jsonElement.TryGetInt32(out var i) ? i : 0,
            IConvertible convertible => Convert.ToInt32(convertible),
            _ => 0
        };
    }
}