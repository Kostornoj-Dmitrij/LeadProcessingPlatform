using System.Diagnostics;
using System.Text.Json;
using AvroSchemas.Messages.LeadEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DistributionService.Application.Common.Interfaces;
using DistributionService.Application.Metrics;
using DistributionService.Domain.Constants;
using DistributionService.Domain.Entities;
using DistributionService.Domain.Enums;
using DistributionService.Infrastructure.Data;
using SharedInfrastructure.Telemetry;
using SharedKernel.Base;
using SharedKernel.Json;

namespace DistributionService.Infrastructure.Background;

/// <summary>
/// Фоновый сервис для асинхронной обработки запросов на распределение лидов
/// </summary>
public class DistributionProcessor(
    IServiceScopeFactory scopeFactory,
    ILogger<DistributionProcessor> logger)
    : BackgroundService
{
    private readonly TimeSpan _pollingInterval = TimeSpan.FromMilliseconds(100);
    private readonly int _batchSize = 100;
    private const int MaxRetryAttempts = 3;
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4),
        TimeSpan.FromSeconds(8)
    ];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Distribution Processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingRequests(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing distribution requests");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        logger.LogInformation("Distribution Processor stopped");
    }

    private async Task ProcessPendingRequests(CancellationToken cancellationToken)
    {
        using var readScope = scopeFactory.CreateScope();
        var readContext = readScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var now = DateTime.UtcNow;

        var requests = await readContext.Set<DistributionRequest>()
            .Where(x => x.Status == DistributionRequestStatus.Pending ||
                        (x.Status == DistributionRequestStatus.Failed &&
                         x.RetryCount < MaxRetryAttempts &&
                         x.NextRetryAt != null &&
                         x.NextRetryAt <= now))
            .OrderBy(x => x.LastAttemptAt)
            .Take(_batchSize)
            .ToListAsync(cancellationToken);

        if (!requests.Any())
            return;

        logger.LogInformation("Found {Count} distribution requests ready for processing", requests.Count);

        foreach (var request in requests)
        {
            await ProcessSingleRequestAsync(request, cancellationToken);
        }
    }

    private async Task ProcessSingleRequestAsync(DistributionRequest request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        using var activity = ActivityBuilder.RestoreAndCreateActivity(
                TelemetrySpanNames.DistributionProcess,
                request.TraceParent)
            .WithTag(TelemetryAttributes.LeadId, request.LeadId)
            .WithDistributionProcessorTags(
                request.Id,
                request.CompanyName,
                request.RetryCount + 1,
                MaxRetryAttempts);

        using var scope = scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var targetClient = scope.ServiceProvider.GetRequiredService<IDistributionTargetClient>();

        var freshRequest = await unitOfWork.Set<DistributionRequest>()
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (freshRequest == null)
        {
            logger.LogWarning("Distribution request {RequestId} not found", request.Id);
            return;
        }

        logger.LogInformation(
            "Processing distribution request {RequestId} for lead {LeadId} (Attempt {Attempt}/{MaxRetries})",
            freshRequest.Id, freshRequest.LeadId, freshRequest.RetryCount + 1, MaxRetryAttempts);

        try
        {
            freshRequest.StartProcessing();
            await unitOfWork.SaveChangesAsync(cancellationToken);

            DistributionMetrics.DistributionRequests.Add(1, new TagList { { "status", "processing" } });

            var customFields = freshRequest.CustomFields != null
                ? new Dictionary<string, string>(freshRequest.CustomFields)
                : new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(freshRequest.EnrichedData))
            {
                try
                {
                    var enrichedData = JsonSerializer.Deserialize<EnrichedDataDto>(
                        freshRequest.EnrichedData,
                        JsonDefaults.Options);

                    if (enrichedData != null)
                    {
                        customFields[RuleConfigKeys.CustomFieldIndustry] = enrichedData.Industry;
                        customFields[RuleConfigKeys.CustomFieldCompanySize] = enrichedData.CompanySize;
                        if (enrichedData.Website != null)
                            customFields[RuleConfigKeys.CustomFieldWebsite] = enrichedData.Website;
                        if (enrichedData.RevenueRange != null)
                            customFields[RuleConfigKeys.CustomFieldRevenueRange] = enrichedData.RevenueRange;
                    }
                }
                catch (JsonException ex)
                {
                    logger.LogWarning(ex, "Failed to parse enriched data for lead {LeadId}", freshRequest.LeadId);
                }
            }

            var result = await targetClient.SendAsync(
                freshRequest.LeadId,
                freshRequest.CompanyName,
                freshRequest.Email,
                freshRequest.Score,
                customFields,
                freshRequest.Target ?? string.Empty,
                cancellationToken);

            if (result.IsSuccess)
            {
                DistributionMetrics.DistributionSuccess.Add(1, new TagList { { "target", freshRequest.Target ?? "unknown" } });

                var history = DistributionHistory.CreateSuccess(
                    freshRequest.LeadId,
                    freshRequest.RuleId,
                    freshRequest.Target ?? string.Empty,
                    result.ResponseData);

                await unitOfWork.Set<DistributionHistory>().AddAsync(history, cancellationToken);
                freshRequest.MarkCompleted();

                await unitOfWork.SaveChangesAsync(cancellationToken);

                DistributionMetrics.DistributionRequests.Add(1, new TagList { { "status", "completed" } });
                DistributionMetrics.DistributionDuration.Record(stopwatch.Elapsed.TotalMilliseconds,
                    new TagList { { "target", freshRequest.Target ?? "unknown" }, { "success", "true" } });
                logger.LogInformation(
                    "Successfully distributed lead {LeadId} to {Target}",
                    freshRequest.LeadId, freshRequest.Target);
            }
            else
            {
                await HandleFailedRequest(freshRequest, result.ErrorMessage ?? "Unknown error", unitOfWork, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during distribution for lead {LeadId}", freshRequest.LeadId);
            await HandleFailedRequest(freshRequest, $"Exception: {ex.Message}", unitOfWork, cancellationToken);
        }
    }

    private async Task HandleFailedRequest(
        DistributionRequest request,
        string errorMessage,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var nextRetryAt = CalculateNextRetryTime(request.RetryCount);

        if (request.RetryCount < MaxRetryAttempts)
        {
            request.MarkFailed(errorMessage, nextRetryAt);
            DistributionMetrics.DistributionRetry.Add(1, new TagList { { "attempt", (request.RetryCount + 1).ToString() } });
        }
        else
        {
            request.MarkFailed(errorMessage);

            DistributionMetrics.DistributionRequests.Add(1, new TagList { { "status", "failed" } });

            var history = DistributionHistory.CreateFailed(
                request.LeadId,
                request.RuleId,
                errorMessage,
                request.Target);

            await unitOfWork.Set<DistributionHistory>().AddAsync(history, cancellationToken);

            var errorType = errorMessage.Contains("timeout") ? "timeout" :
                errorMessage.Contains("Forced") ? "forced_failure" : "unknown";
            DistributionMetrics.DistributionFailure.Add(1, new TagList
                { { "target", request.Target ?? "unknown" }, { "error_type", errorType } });

            logger.LogWarning(
                "Distribution failed permanently for lead {LeadId} after {RetryCount} attempts. Error: {Error}",
                request.LeadId, request.RetryCount, errorMessage);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private DateTime CalculateNextRetryTime(int retryCount)
    {
        if (retryCount >= MaxRetryAttempts)
            return DateTime.MaxValue;

        var delay = RetryDelays[Math.Min(retryCount, RetryDelays.Length - 1)];
        return DateTime.UtcNow.Add(delay);
    }
}