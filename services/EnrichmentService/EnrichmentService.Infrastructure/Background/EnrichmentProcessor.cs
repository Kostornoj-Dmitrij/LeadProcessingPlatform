using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using EnrichmentService.Application.Common.Interfaces;
using EnrichmentService.Domain.Entities;
using EnrichmentService.Domain.Enums;
using EnrichmentService.Infrastructure.Data;
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
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(3);
    private readonly int _batchSize = 10;
    private const int MaxRetryAttempts = 3;

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
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var enrichmentClient = scope.ServiceProvider.GetRequiredService<IExternalEnrichmentClient>();

        var requests = await context.EnrichmentRequests
            .Where(x => x.Status == EnrichmentRequestStatus.Pending || 
                        x.Status == EnrichmentRequestStatus.Failed)
            .OrderBy(x => x.LastAttemptAt)
            .Take(_batchSize)
            .ToListAsync(cancellationToken);

        var pendingRequests = requests
            .Where(x => x.IsReadyForProcessing(MaxRetryAttempts))
            .ToList();

        if (!pendingRequests.Any())
            return;

        logger.LogInformation("Processing {Count} enrichment requests", pendingRequests.Count);

        foreach (var request in pendingRequests)
        {
            request.StartProcessing();
            
            try
            {
                var result = await enrichmentClient.EnrichAsync(
                    request.CompanyName,
                    request.CustomFields,
                    cancellationToken);

                if (result.IsSuccess)
                {
                    var enrichmentResult = EnrichmentResult.Create(
                        request.LeadId,
                        request.CompanyName,
                        result.Industry!,
                        result.CompanySize!,
                        result.Website,
                        result.RevenueRange,
                        result.RawResponse);

                    await unitOfWork.Set<EnrichmentResult>().AddAsync(enrichmentResult, cancellationToken);

                    request.MarkCompleted();
                }
                else
                {
                    request.MarkFailed(result.ErrorMessage!);
                }
            }
            catch (Exception ex)
            {
                request.MarkFailed($"Exception during enrichment: {ex.Message}");
                logger.LogError(ex, "Unexpected error during enrichment for lead {LeadId}", request.LeadId);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}