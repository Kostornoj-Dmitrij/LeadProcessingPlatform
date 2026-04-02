using System.Diagnostics;
using System.Text.Json;
using AvroSchemas.Messages.LeadEvents;
using DistributionService.Application.Common.DTOs;
using DistributionService.Application.Metrics;
using DistributionService.Domain.Entities;
using DistributionService.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedInfrastructure.Telemetry;
using SharedKernel.Base;
using IDistributionTargetClient = DistributionService.Application.Common.Interfaces.IDistributionTargetClient;

namespace DistributionService.Application.EventHandlers;

/// <summary>
/// Обработчик события LeadQualified
/// </summary>
public class LeadQualifiedEventHandler(
    IUnitOfWork unitOfWork,
    IDistributionTargetClient targetClient,
    ILogger<LeadQualifiedEventHandler> logger)
    : INotificationHandler<LeadQualified>
{
    private const int MaxRetryAttempts = 3;

    public async Task Handle(LeadQualified @event, CancellationToken cancellationToken)
    {
        using var activity = TelemetryConstants.ActivitySource.StartEventHandlerSpan("LeadQualified")!
            .AddTags(
                (TelemetryAttributes.LeadId, @event.LeadId),
                (TelemetryAttributes.EventName, "LeadQualified"),
                (TelemetryAttributes.LeadScore, @event.Score),
                (TelemetryAttributes.LeadCompany, @event.CompanyName),
                (TelemetryAttributes.LeadEmail, @event.Email),
                (TelemetryAttributes.ProcessingStep, "distribution_start"));
        logger.LogInformation("Processing LeadQualified for lead {LeadId} with score {Score}",
            @event.LeadId, @event.Score);

        var rules = await unitOfWork.Set<DistributionRule>()
            .Where(r => r.IsActive && (r.ValidTo == null || r.ValidTo > DateTime.UtcNow))
            .OrderBy(r => r.Priority)
            .ToListAsync(cancellationToken);

        if (!rules.Any())
        {
            logger.LogWarning("No active distribution rules found for lead {LeadId}", @event.LeadId);
            await RecordFailure(@event, null, "No active distribution rules found", cancellationToken);
            return;
        }

        DistributionRule? applicableRule = null;

        foreach (var rule in rules)
        {
            if (await IsRuleApplicableAsync(rule, @event))
            {
                applicableRule = rule;
                break;
            }
        }

        if (applicableRule == null)
        {
            logger.LogWarning("No applicable distribution rule found for lead {LeadId}", @event.LeadId);
            await RecordFailure(@event, null, "No applicable distribution rule found", cancellationToken);
            return;
        }

        var target = ResolveTarget(applicableRule, @event);

        if (string.IsNullOrEmpty(target))
        {
            logger.LogWarning("Failed to resolve target for lead {LeadId} using rule {RuleName}", 
                @event.LeadId, applicableRule.RuleName);
            await RecordFailure(@event, applicableRule.Id, "Failed to resolve distribution target", cancellationToken);
            return;
        }

        var customFields = @event.CustomFields != null
            ? new Dictionary<string, string>(@event.CustomFields)
            : new Dictionary<string, string>();
        logger.LogInformation("CustomFields for lead {LeadId}: {@CustomFields}", @event.LeadId, customFields);
        if (@event.EnrichedData != null)
        {
            customFields["industry"] = @event.EnrichedData.Industry;
            customFields["company_size"] = @event.EnrichedData.CompanySize;
            if (@event.EnrichedData.Website != null)
                customFields["website"] = @event.EnrichedData.Website;
            if (@event.EnrichedData.RevenueRange != null)
                customFields["revenue_range"] = @event.EnrichedData.RevenueRange;
        }

        DistributionMetrics.DistributionAttempts.Add(1, new TagList 
            { { "target", target }, { "rule_name", applicableRule.RuleName } });

        var result = await SendWithRetryAsync(
            @event.LeadId,
            @event.CompanyName,
            @event.Email,
            @event.Score,
            customFields,
            target,
            cancellationToken);

        if (result.IsSuccess)
        {
            var history = DistributionHistory.CreateSuccess(
                @event.LeadId,
                applicableRule.Id,
                target,
                result.ResponseData);

            await unitOfWork.Set<DistributionHistory>().AddAsync(history, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Lead {LeadId} successfully distributed to {Target} using rule {RuleName}",
                @event.LeadId, target, applicableRule.RuleName);
        }
        else
        {
            await RecordFailure(@event, applicableRule.Id, result.ErrorMessage ?? "Unknown error", cancellationToken);

            logger.LogError(
                "Failed to distribute lead {LeadId} to {Target} using rule {RuleName}. Error: {Error}",
                @event.LeadId, target, applicableRule.RuleName, result.ErrorMessage);
        }
    }

    private Task<bool> IsRuleApplicableAsync(DistributionRule rule, LeadQualified @event)
    {
        try
        {
            var condition = JsonSerializer.Deserialize<Dictionary<string, object>>(rule.ConditionJson);
            if (condition == null)
                return Task.FromResult(false);

            if (!condition.TryGetValue("type", out var typeObj))
                return Task.FromResult(false);

            var type = typeObj.ToString();

            var result = type switch
            {
                "score_threshold" => EvaluateScoreThreshold(condition, @event.Score),
                "industry_match" => EvaluateIndustryMatch(condition, @event.EnrichedData?.Industry),
                "revenue_range" => EvaluateRevenueRange(condition, @event.EnrichedData?.RevenueRange),
                "always_true" => true,
                _ => false
            };

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error evaluating rule {RuleName} for lead {LeadId}",
                rule.RuleName, @event.LeadId);
            return Task.FromResult(false);
        }
    }

    private bool EvaluateScoreThreshold(Dictionary<string, object> condition, int score)
    {
        if (!condition.TryGetValue("min_score", out var minScoreObj))
            return true;

        if (int.TryParse(minScoreObj.ToString(), out var minScore))
            return score >= minScore;

        return true;
    }

    private bool EvaluateIndustryMatch(Dictionary<string, object> condition, string? industry)
    {
        if (string.IsNullOrEmpty(industry))
            return false;

        if (!condition.TryGetValue("industry", out var industryObj))
            return false;

        var targetIndustry = industryObj.ToString();
        return industry.Equals(targetIndustry, StringComparison.OrdinalIgnoreCase);
    }

    private bool EvaluateRevenueRange(Dictionary<string, object> condition, string? revenueRange)
    {
        if (string.IsNullOrEmpty(revenueRange))
            return false;

        if (!condition.TryGetValue("range", out var rangeObj))
            return false;

        return revenueRange.Equals(rangeObj.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private string ResolveTarget(DistributionRule rule, LeadQualified @event)
    {
        try
        {
            var config = JsonSerializer.Deserialize<Dictionary<string, object>>(rule.TargetConfigJson);
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
            territories = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonElement.GetRawText()) ?? new();
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
            specializations = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonElement.GetRawText()) ?? new();
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

        if (thresholdsObj is JsonElement jsonElement)
        {
            thresholds = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonElement.GetRawText()) ?? new();
        }
        else if (thresholdsObj is List<object> list)
        {
            thresholds = list.Cast<Dictionary<string, object>>().ToList();
        }
        else
        {
            return config.GetValueOrDefault("default_target")?.ToString() ?? string.Empty;
        }

        foreach (var threshold in thresholds.OrderByDescending(t =>
            Convert.ToInt32(t.GetValueOrDefault("min_score", 0))))
        {
            if (score >= Convert.ToInt32(threshold.GetValueOrDefault("min_score", 0)))
                return threshold.GetValueOrDefault("target")?.ToString() ?? string.Empty;
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
            targets = JsonSerializer.Deserialize<List<string>>(jsonElement.GetRawText()) ?? new();
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

    private async Task<DistributionResult> SendWithRetryAsync(
        Guid leadId,
        string companyName,
        string email,
        int score,
        Dictionary<string, string>? customFields,
        string target,
        CancellationToken cancellationToken)
    {
        int attempt = 0;
        var overallStopwatch = Stopwatch.StartNew();

        while (attempt < MaxRetryAttempts)
        {
            var attemptStopwatch = Stopwatch.StartNew();

            try
            {
                var result = await targetClient.SendAsync(
                    leadId, companyName, email, score, customFields, target, cancellationToken);

                if (result.IsSuccess)
                {
                    DistributionMetrics.DistributionSuccess.Add(1, new TagList { { "target", target } });
                    DistributionMetrics.DistributionDuration.Record(attemptStopwatch.Elapsed.TotalMilliseconds, 
                        new TagList { { "target", target }, { "success", "true" } });
                    return result;
                }

                attempt++;
                if (attempt < MaxRetryAttempts)
                {
                    DistributionMetrics.DistributionRetry.Add(1, new TagList { { "attempt", attempt.ToString() } });

                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    logger.LogWarning(
                        "Retry {Attempt}/{MaxRetries} for lead {LeadId} to target {Target}",
                        attempt, MaxRetryAttempts, leadId, target);
                    await Task.Delay(delay, cancellationToken);
                }
                else
                {
                    var errorType = result.ErrorMessage?.Contains("timeout") == true ? "timeout" : "unknown";
                    DistributionMetrics.DistributionFailure.Add(1, new TagList 
                        { { "target", target }, { "error_type", errorType } });
                    DistributionMetrics.DistributionDuration.Record(overallStopwatch.Elapsed.TotalMilliseconds, 
                        new TagList { { "target", target }, { "success", "false" } });
                }
            }
            catch (Exception ex)
            {
                attempt++;
                logger.LogWarning(ex, "Attempt {Attempt}/{MaxRetries} failed for lead {LeadId}",
                    attempt, MaxRetryAttempts, leadId);

                if (attempt >= MaxRetryAttempts)
                {
                    var errorType = ex.Message.Contains("timeout") ? "timeout" : "unknown";
                    DistributionMetrics.DistributionFailure.Add(1, new TagList 
                        { { "target", target }, { "error_type", errorType } });
                    DistributionMetrics.DistributionDuration.Record(overallStopwatch.Elapsed.TotalMilliseconds, 
                        new TagList { { "target", target }, { "success", "false" } });
                    
                    return new DistributionResult(false, null, ex.Message);
                }
            }
        }

        return new DistributionResult(false, null, $"Failed after {MaxRetryAttempts} attempts");
    }

    private async Task RecordFailure(
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
}