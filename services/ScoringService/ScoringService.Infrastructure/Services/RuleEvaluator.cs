using System.Diagnostics;
using System.Text.Json;
using AvroSchemas.Messages.LeadEvents;
using Microsoft.Extensions.Logging;
using ScoringService.Application.Metrics;
using ScoringService.Application.Services;
using ScoringService.Domain.Entities;

namespace ScoringService.Infrastructure.Services;

/// <summary>
/// Реализация оценки правил скоринга
/// </summary>
public class RuleEvaluator(ILogger<RuleEvaluator> logger) : IRuleEvaluator
{
    public async Task<bool> EvaluateAsync(
        ScoringRule rule, 
        ScoringRequest request, 
        EnrichedDataDto? enrichedData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var condition = JsonSerializer.Deserialize<Dictionary<string, object>>(rule.ConditionJson);
            if (condition == null)
            {
                logger.LogWarning("Invalid condition format for rule {RuleName}", rule.RuleName);
                return false;
            }

            if (!condition.TryGetValue("type", out var typeObj))
            {
                logger.LogWarning("Rule {RuleName} missing 'type' field", rule.RuleName);
                return false;
            }

            var type = typeObj.ToString();
            var result = type switch
            {
                "field_equals" => EvaluateFieldEquals(condition, request, enrichedData),
                "field_contains" => EvaluateFieldContains(condition, request, enrichedData),
                "custom_field_equals" => EvaluateCustomFieldEquals(condition, request),
                "score_threshold" => EvaluateScoreThreshold(condition, request),
                "always_true" => true,
                _ => false
            };

            ScoringMetrics.RulesEvaluated.Add(1, new TagList { { "rule_name", rule.RuleName } });
            logger.LogDebug(
                "Rule {RuleName} evaluated to {Result} for lead {LeadId}",
                rule.RuleName, result, request.LeadId);

            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error evaluating rule {RuleName} for lead {LeadId}", 
                rule.RuleName, request.LeadId);
            return false;
        }
    }

    private bool EvaluateFieldEquals(
        Dictionary<string, object> condition,
        ScoringRequest request,
        EnrichedDataDto? enrichedData)
    {
        if (!condition.TryGetValue("field", out var fieldObj) ||
            !condition.TryGetValue("value", out var valueObj))
        {
            return false;
        }

        var field = fieldObj.ToString();
        var expectedValue = valueObj.ToString();

        return field switch
        {
            "industry" => enrichedData?.Industry == expectedValue,
            "company_size" => enrichedData?.CompanySize == expectedValue,
            "revenue_range" => enrichedData?.RevenueRange == expectedValue,
            "company_name" => request.CompanyName == expectedValue,
            "email" => request.Email == expectedValue,
            _ => false
        };
    }

    private bool EvaluateFieldContains(
        Dictionary<string, object> condition,
        ScoringRequest request,
        EnrichedDataDto? enrichedData)
    {
        if (!condition.TryGetValue("field", out var fieldObj) ||
            !condition.TryGetValue("value", out var valueObj))
        {
            return false;
        }

        var field = fieldObj.ToString();
        var expectedValue = valueObj.ToString();

        return field switch
        {
            "industry" => enrichedData?.Industry.Contains(expectedValue ?? "") == true,
            "company_name" => request.CompanyName.Contains(expectedValue ?? "") == true,
            _ => false
        };
    }

    private bool EvaluateCustomFieldEquals(
        Dictionary<string, object> condition,
        ScoringRequest request)
    {
        if (!condition.TryGetValue("field_name", out var fieldNameObj) ||
            !condition.TryGetValue("value", out var valueObj))
        {
            return false;
        }

        var fieldName = fieldNameObj.ToString();
        var expectedValue = valueObj.ToString();

        return request.CustomFields != null &&
               request.CustomFields.TryGetValue(fieldName ?? "", out var actualValue) &&
               actualValue == expectedValue;
    }

    private bool EvaluateScoreThreshold(
        Dictionary<string, object> condition,
        ScoringRequest request)
    {
        // Для оценки порога скоринга - эмуляция
        return false;
    }
}