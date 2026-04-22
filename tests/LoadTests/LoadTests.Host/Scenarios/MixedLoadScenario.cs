using System.Text.Json;
using LoadTests.Host.Infrastructure;
using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;

namespace LoadTests.Host.Scenarios;

/// <summary>
/// Смешанный сценарий с распределением вероятностей разных типов лидов
/// </summary>
public static class MixedLoadScenario
{
    private static readonly HttpClient HttpClient = new();
    private static LeadGenerator? _generator;

    public static ScenarioProps Create(
        string apiGatewayUrl, 
        int targetRps, 
        int durationSeconds, 
        LeadGenerator generator,
        int successWeight = 70,
        int enrichmentFailureWeight = 10,
        int scoringFailureWeight = 10,
        int distributionFailureWeight = 10)
    {
        _generator = generator;
        var totalWeight = successWeight + enrichmentFailureWeight + scoringFailureWeight + distributionFailureWeight;

        return Scenario.Create("mixed_load", async context =>
            {
                var token = await AuthHelper.GetTokenAsync(apiGatewayUrl);

                var random = Random.Shared.Next(totalWeight);

                object lead;
                ExpectedScenarioPath expectedPath;

                if (random < successWeight)
                {
                    lead = TestData.CreateSuccessLead();
                    expectedPath = ExpectedScenarioPath.Success;
                }
                else if (random < successWeight + enrichmentFailureWeight)
                {
                    lead = TestData.CreateEnrichmentFailureLead();
                    expectedPath = ExpectedScenarioPath.EnrichmentFailure;
                }
                else if (random < successWeight + enrichmentFailureWeight + scoringFailureWeight)
                {
                    lead = TestData.CreateScoringFailureLead();
                    expectedPath = ExpectedScenarioPath.ScoringFailure;
                }
                else
                {
                    lead = TestData.CreateDistributionFailureLead();
                    expectedPath = ExpectedScenarioPath.DistributionFailure;
                }

                using var request = Http.CreateRequest("POST", $"{apiGatewayUrl}/api/leads")
                    .WithHeader("Content-Type", "application/json")
                    .WithHeader("Authorization", $"Bearer {token}")
                    .WithHeader("Idempotency-Key", Guid.NewGuid().ToString())
                    .WithBody(lead.ToJsonContent());

                var response = await Http.Send(HttpClient, request);

                if (response.StatusCode is not ("202" or "Accepted"))
                    return Response.Fail(statusCode: response.StatusCode, message: $"HTTP {response.StatusCode}");
                try
                {
                    var content = await response.Payload.Value.Content.ReadAsStringAsync();
                    var json = JsonDocument.Parse(content);
                    if (json.RootElement.TryGetProperty("id", out var idElement))
                    {
                        var leadId = idElement.GetGuid();
                        _generator?.TrackCreatedLead(leadId, expectedPath);
                    }
                }
                catch (Exception ex)
                {
                    context.Logger.Warning($"Failed to parse lead ID from response: {ex.Message}");
                }

                return Response.Ok(statusCode: response.StatusCode);
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: targetRps,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromSeconds(durationSeconds))
            );
    }
}