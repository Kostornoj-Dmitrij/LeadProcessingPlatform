using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScoringService.Domain.Entities;
using ScoringService.Domain.Enums;
using ScoringService.Infrastructure.Data;
using SharedKernel.Base;
using System.Text.Json;
using AvroSchemas.Messages.LeadEvents;
using ScoringService.Application.Services;

namespace ScoringService.Infrastructure.Background;

/// <summary>
/// Фоновый сервис для асинхронной обработки запросов на скоринг лидов
/// </summary>
public class ScoringProcessor(
    IServiceScopeFactory scopeFactory,
    ILogger<ScoringProcessor> logger)
    : BackgroundService
{
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(3);
    private readonly int _batchSize = 10;
    private const int MaxRetryAttempts = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Scoring Processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingRequests(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing scoring requests");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        logger.LogInformation("Scoring Processor stopped");
    }

    private async Task ProcessPendingRequests(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var ruleEvaluator = scope.ServiceProvider.GetRequiredService<IRuleEvaluator>();

        var requests = await context.ScoringRequests
            .Where(x => x.Status == ScoringRequestStatus.Pending ||
                        x.Status == ScoringRequestStatus.Failed)
            .OrderBy(x => x.LastAttemptAt)
            .Take(_batchSize)
            .ToListAsync(cancellationToken);

        var pendingRequests = requests
            .Where(x => x.IsReadyForProcessing(MaxRetryAttempts))
            .ToList();

        if (!pendingRequests.Any()) return;

        logger.LogInformation("Processing {Count} scoring requests", pendingRequests.Count);

        var rules = await context.ScoringRules
            .Where(r => r.IsActive && 
                        (r.ValidTo == null || r.ValidTo > DateTime.UtcNow))
            .OrderBy(r => r.Priority)
            .ToListAsync(cancellationToken);

        foreach (var request in pendingRequests)
        {
            request.StartProcessing();

            try
            {
                EnrichedDataDto? enrichedData = null;
                if (!string.IsNullOrEmpty(request.EnrichedData))
                {
                    try
                    {
                        enrichedData = JsonSerializer.Deserialize<EnrichedDataDto>(
                            request.EnrichedData, 
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                    catch (JsonException ex)
                    {
                        logger.LogWarning(ex, "Failed to parse enriched data for lead {LeadId}", request.LeadId);
                    }
                }

                int totalScore = 0;
                var appliedRules = new List<string>();

                foreach (var rule in rules)
                {
                    if (await ruleEvaluator.EvaluateAsync(rule, request, enrichedData, cancellationToken))
                    {
                        totalScore += rule.ScoreValue;
                        appliedRules.Add(rule.RuleName);
                    }
                }

                if (request.CustomFields != null && 
                    request.CustomFields.TryGetValue("forceScoringFail", out var forceFail) && 
                    forceFail == "true")
                {
                    throw new InvalidOperationException("Forced scoring failure for testing");
                }

                if (request.CustomFields != null && 
                    request.CustomFields.TryGetValue("score", out var customScore) && 
                    int.TryParse(customScore, out var parsedScore))
                {
                    totalScore = parsedScore;
                }

                int qualifiedThreshold = 25;

                request.MarkCompleted(totalScore, qualifiedThreshold, appliedRules);

                var scoringResult = ScoringResult.Create(
                    request.LeadId,
                    totalScore,
                    qualifiedThreshold,
                    appliedRules);

                await unitOfWork.Set<ScoringResult>().AddAsync(scoringResult, cancellationToken);

                logger.LogInformation(
                    "Lead {LeadId} scored with total {TotalScore} points (threshold: {Threshold})", 
                    request.LeadId, totalScore, qualifiedThreshold);
            }
            catch (Exception ex)
            {
                request.MarkFailed($"Exception during scoring: {ex.Message}");
                logger.LogError(ex, "Unexpected error during scoring for lead {LeadId}", request.LeadId);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}