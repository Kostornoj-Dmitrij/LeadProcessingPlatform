using System.Diagnostics;
using System.Text;
using System.Text.Json;
using DistributionService.Application.Common.DTOs;
using DistributionService.Application.Common.Interfaces;
using DistributionService.Application.Metrics;
using DistributionService.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedInfrastructure.Telemetry;

namespace DistributionService.Infrastructure.Clients;

/// <summary>
/// Эмулятор клиента для отправки лидов в целевую систему
/// </summary>
public class DistributionTargetClient(
    HttpClient httpClient,
    IOptions<DistributionOptions> options,
    ILogger<DistributionTargetClient> logger)
    : IDistributionTargetClient
{
    private readonly DistributionOptions _options = options.Value;

    public async Task<DistributionResult> SendAsync(
        Guid leadId,
        string companyName,
        string email,
        int score,
        Dictionary<string, string>? customFields,
        string target,
        CancellationToken cancellationToken = default)
    {
        using var activity = TelemetryConstants.ActivitySource.StartActivity(
                TelemetrySpanNames.DistributionClient,
                ActivityKind.Client)!
            .AddTags(
                (TelemetryAttributes.LeadId, leadId),
                (TelemetryAttributes.DistributionTarget, target),
                (TelemetryAttributes.DistributionCompany, companyName),
                (TelemetryAttributes.DistributionScore, score));

        logger.LogInformation("Sending lead {LeadId} to target system: {Target}", leadId, target);

        await Task.Delay(50, cancellationToken);

        if (customFields != null &&
            customFields.TryGetValue("forceDistributionFail", out var forceFail) &&
            forceFail == "true")
        {
            logger.LogWarning("Forced distribution failure for lead {LeadId}", leadId);
            activity!.SetTag(TelemetryAttributes.DistributionForcedFailure, true);
            return new DistributionResult(false, null, "Forced distribution failure for testing");
        }

        if (customFields != null &&
            customFields.TryGetValue("distributionHttpStatus", out var httpStatusStr) &&
            int.TryParse(httpStatusStr, out var httpStatus) &&
            httpStatus >= 400)
        {
            logger.LogWarning("Forced HTTP {HttpStatus} for lead {LeadId}", httpStatus, leadId);
            activity!.SetTag(TelemetryAttributes.HttpStatusCode, httpStatus);
            activity.SetTag(TelemetryAttributes.DistributionForcedFailure, true);
            return new DistributionResult(false, null, $"HTTP {httpStatus}: Forced error response");
        }

        var request = new
        {
            LeadId = leadId,
            CompanyName = companyName,
            Email = email,
            Score = score,
            CustomFields = customFields,
            Target = target,
            Timestamp = DateTime.UtcNow
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        try
        {
            if (string.IsNullOrEmpty(_options.TargetApiUrl))
            {
                logger.LogInformation("No TargetApiUrl configured, using emulation mode for lead {LeadId}", leadId);

                var emulationResponse = new
                {
                    success = true,
                    message = "Lead received (emulation mode)",
                    leadId = leadId,
                    target = target,
                    processedAt = DateTime.UtcNow
                };

                var responseJson = JsonSerializer.Serialize(emulationResponse);
                activity!.SetTag(TelemetryAttributes.DistributionMode, "emulation");
                return new DistributionResult(true, responseJson, null);
            }

            var response = await httpClient.PostAsync($"{_options.TargetApiUrl}/api/distribution", content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogInformation("Lead {LeadId} successfully sent to {Target}. Response: {Response}",
                    leadId, target, responseContent);
                activity!.SetTag(TelemetryAttributes.HttpStatusCode, (int)response.StatusCode);
                activity.SetTag(TelemetryAttributes.DistributionSuccess, true);

                DistributionMetrics.DistributionHttpStatusCodes.Add(1, new TagList 
                    { { "status_code", ((int)response.StatusCode).ToString() }, { "target", target } });
                return new DistributionResult(true, responseContent, null);
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("Failed to send lead {LeadId} to {Target}. Status: {Status}, Error: {Error}",
                leadId, target, response.StatusCode, error);
            activity!.SetTag(TelemetryAttributes.HttpStatusCode, (int)response.StatusCode);
            activity.SetTag(TelemetryAttributes.DistributionSuccess, false);
            activity.SetTag(TelemetryAttributes.Error, error);

            DistributionMetrics.DistributionHttpStatusCodes.Add(1, new TagList 
                { { "status_code", ((int)response.StatusCode).ToString() }, { "target", target } });
            var errorType = (int)response.StatusCode >= 500 ? "http_5xx" : "http_4xx";
            DistributionMetrics.DistributionFailure.Add(1, new TagList 
                { { "target", target }, { "error_type", errorType } });

            return new DistributionResult(false, null, $"HTTP {response.StatusCode}: {error}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending lead {LeadId} to {Target}", leadId, target);
            activity!.SetTag(TelemetryAttributes.DistributionSuccess, false);
            activity.SetTag(TelemetryAttributes.Error, ex.Message);
            activity.SetTag(TelemetryAttributes.ErrorType, ex.GetType().Name);
            return new DistributionResult(false, null, ex.Message);
        }
    }
}