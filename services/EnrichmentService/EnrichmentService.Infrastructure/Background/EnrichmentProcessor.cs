using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using EnrichmentService.Application.Common.Interfaces;
using EnrichmentService.Application.Metrics;
using EnrichmentService.Domain.Entities;
using EnrichmentService.Domain.Enums;
using EnrichmentService.Infrastructure.Data;
using SharedInfrastructure.Telemetry;
using SharedKernel.Base;

namespace EnrichmentService.Infrastructure.Background;

/// <summary>
/// Фоновый сервис для асинхронной обработки запросов на обогащение лидов
/// </summary>
public class EnrichmentProcessor(
    IServiceScopeFactory scopeFactory,
    ILogger<EnrichmentProcessor> logger)
    : BackgroundService
{
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);
    private readonly int _batchSize = 10;
    private const int MaxRetryAttempts = 3;
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromMinutes(2)
    ];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Enrichment Processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingRequests(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing enrichment requests");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        logger.LogInformation("Enrichment Processor stopped");
    }

    private async Task ProcessPendingRequests(CancellationToken cancellationToken)
    {
        using var readScope = scopeFactory.CreateScope();
        var readContext = readScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var now = DateTime.UtcNow;

        var requests = await readContext.EnrichmentRequests
            .Where(x => x.Status == EnrichmentRequestStatus.Pending ||
                        (x.Status == EnrichmentRequestStatus.Failed &&
                         x.RetryCount < MaxRetryAttempts &&
                         x.NextRetryAt != null &&
                         x.NextRetryAt <= now))
            .OrderBy(x => x.LastAttemptAt)
            .Take(_batchSize)
            .ToListAsync(cancellationToken);

        if (!requests.Any())
            return;

        logger.LogInformation("Found {Count} enrichment requests ready for processing", requests.Count);

        foreach (var request in requests)
        {
            await ProcessSingleRequestAsync(request, cancellationToken);
        }
    }

    private async Task ProcessSingleRequestAsync(EnrichmentRequest request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        using var activity = TelemetryRestorer.RestoreAndStartActivity(
                TelemetryConstants.ActivitySource,
                TelemetrySpanNames.EnrichmentProcess,
                request.TraceParent)!
            .AddTags(
                (TelemetryAttributes.LeadId, request.LeadId),
                (TelemetryAttributes.EnrichmentRequestId, request.Id),
                (TelemetryAttributes.EnrichmentCompanyName, request.CompanyName),
                (TelemetryAttributes.EnrichmentAttempt, request.RetryCount + 1),
                (TelemetryAttributes.EnrichmentMaxRetries, MaxRetryAttempts));

        using var scope = scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var enrichmentClient = scope.ServiceProvider.GetRequiredService<IExternalEnrichmentClient>();

        var freshRequest = await unitOfWork.Set<EnrichmentRequest>()
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (freshRequest == null)
        {
            logger.LogWarning("Enrichment request {RequestId} not found", request.Id);
            return;
        }

        logger.LogInformation(
            "Processing enrichment request {RequestId} for lead {LeadId} (Attempt {Attempt}/{MaxRetries})",
            freshRequest.Id, freshRequest.LeadId, freshRequest.RetryCount + 1, MaxRetryAttempts);

        try
        {
            freshRequest.StartProcessing();
            await unitOfWork.SaveChangesAsync(cancellationToken);

            EnrichmentMetrics.EnrichmentRequests.Add(1, new TagList { { "status", "processing" } });

            var result = await enrichmentClient.EnrichAsync(
                freshRequest.CompanyName,
                freshRequest.CustomFields,
                cancellationToken);

            if (result.IsSuccess)
            {
                EnrichmentMetrics.EnrichmentSuccess.Add(1);

                var enrichmentResult = EnrichmentResult.Create(
                    freshRequest.LeadId,
                    freshRequest.CompanyName,
                    result.Industry!,
                    result.CompanySize!,
                    result.Website,
                    result.RevenueRange,
                    result.RawResponse);

                await unitOfWork.Set<EnrichmentResult>().AddAsync(enrichmentResult, cancellationToken);
                freshRequest.MarkCompleted();

                await unitOfWork.SaveChangesAsync(cancellationToken);

                EnrichmentMetrics.EnrichmentRequests.Add(1, new TagList { { "status", "completed" } });
                EnrichmentMetrics.EnrichmentDuration.Record(stopwatch.Elapsed.TotalMilliseconds);
                logger.LogInformation(
                    "Successfully enriched lead {LeadId}. Industry: {Industry}, CompanySize: {CompanySize}",
                    freshRequest.LeadId, result.Industry, result.CompanySize);
            }
            else
            {
                await HandleFailedRequest(freshRequest, result.ErrorMessage!, unitOfWork, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during enrichment for lead {LeadId}", freshRequest.LeadId);
            await HandleFailedRequest(freshRequest, $"Exception: {ex.Message}", unitOfWork, cancellationToken);
        }
    }

    private async Task HandleFailedRequest(
        EnrichmentRequest request,
        string errorMessage,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var nextRetryAt = CalculateNextRetryTime(request.RetryCount);

        if (request.RetryCount < MaxRetryAttempts)
        {
            request.MarkFailed(errorMessage, nextRetryAt);
            EnrichmentMetrics.EnrichmentRetry.Add(1, new TagList { { "attempt", (request.RetryCount + 1).ToString() } });
        }
        else
        {
            request.MarkFailed(errorMessage);
            var errorType = errorMessage.Contains("timeout") ? "timeout" :
                errorMessage.Contains("Forced") ? "forced_failure" : "unknown";
            EnrichmentMetrics.EnrichmentFailure.Add(1, new TagList { { "error_type", errorType } });

            EnrichmentMetrics.EnrichmentRequests.Add(1, new TagList { { "status", "failed" } });
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (request.RetryCount >= MaxRetryAttempts)
        {
            logger.LogWarning(
                "Enrichment failed permanently for lead {LeadId} after {RetryCount} attempts. Error: {Error}",
                request.LeadId, request.RetryCount, errorMessage);
        }
        else
        {
            logger.LogWarning(
                "Enrichment failed for lead {LeadId} (attempt {Attempt}/{MaxRetries}). Next retry at {NextRetryAt}. Error: {Error}",
                request.LeadId, request.RetryCount, MaxRetryAttempts, nextRetryAt, errorMessage);
        }
    }

    private DateTime CalculateNextRetryTime(int retryCount)
    {
        if (retryCount >= MaxRetryAttempts)
            return DateTime.MaxValue;

        var delay = RetryDelays[Math.Min(retryCount, RetryDelays.Length - 1)];
        return DateTime.UtcNow.Add(delay);
    }
}