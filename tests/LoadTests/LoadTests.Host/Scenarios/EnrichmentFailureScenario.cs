using System.Text.Json;
using LoadTests.Host.Infrastructure;
using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;

namespace LoadTests.Host.Scenarios;

/// <summary>
/// Сценарий с ошибкой обогащения лидов
/// </summary>
public static class EnrichmentFailureScenario
{
    private static readonly HttpClient HttpClient = new();
    private static LeadGenerator? _generator;

    public static ScenarioProps Create(string apiGatewayUrl, int targetRps, int durationSeconds, LeadGenerator generator)
    {
        _generator = generator;

        return Scenario.Create("enrichment_failure", async context =>
            {
                var token = await AuthHelper.GetTokenAsync(apiGatewayUrl);

                var lead = TestData.CreateEnrichmentFailureLead();

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
                        _generator?.TrackCreatedLead(leadId, ExpectedScenarioPath.EnrichmentFailure);
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