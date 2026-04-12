using System.Text;
using System.Text.Json;
using SharedKernel.Json;

namespace LoadTests.Host.Infrastructure;

/// <summary>
/// Генератор тестовых данных для лидов
/// </summary>
public static class TestData
{
    private static readonly string[] Companies = ["Acme", "TechStart", "GlobalCorp", "InnovateLabs", "DataSystems"];
    private static readonly string[] Industries = ["Technology", "Healthcare", "Finance", "Retail", "Manufacturing"];

    public static object CreateSuccessLead()
    {
        var company = Companies[Random.Shared.Next(Companies.Length)];
        var shortGuid = Guid.NewGuid().ToString("N")[..6];
        var emailGuid = Guid.NewGuid().ToString("N")[..8];

        return new
        {
            source = "load_test",
            companyName = $"{company} {shortGuid}",
            contactPerson = $"Test User {Random.Shared.Next(1000)}",
            email = $"test_{emailGuid}@loadtest.com",
            phone = $"+7-999-{Random.Shared.Next(100, 999)}-{Random.Shared.Next(1000, 9999)}",
            customFields = new Dictionary<string, string>
            {
                ["industry"] = Industries[Random.Shared.Next(Industries.Length)],
                ["priority"] = Random.Shared.Next(1, 5).ToString()
            }
        };
    }

    public static object CreateEnrichmentFailureLead()
    {
        var company = Companies[Random.Shared.Next(Companies.Length)];
        var shortGuid = Guid.NewGuid().ToString("N")[..6];
        var emailGuid = Guid.NewGuid().ToString("N")[..8];

        return new
        {
            source = "load_test",
            companyName = $"{company} {shortGuid}",
            contactPerson = $"Test User {Random.Shared.Next(1000)}",
            email = $"test_{emailGuid}@loadtest.com",
            phone = $"+7-999-{Random.Shared.Next(100, 999)}-{Random.Shared.Next(1000, 9999)}",
            customFields = new Dictionary<string, string>
            {
                ["industry"] = Industries[Random.Shared.Next(Industries.Length)],
                ["forceEnrichmentFail"] = "true"
            }
        };
    }

    public static object CreateScoringFailureLead()
    {
        var company = Companies[Random.Shared.Next(Companies.Length)];
        var shortGuid = Guid.NewGuid().ToString("N")[..6];
        var emailGuid = Guid.NewGuid().ToString("N")[..8];

        return new
        {
            source = "load_test",
            companyName = $"{company} {shortGuid}",
            contactPerson = $"Test User {Random.Shared.Next(1000)}",
            email = $"test_{emailGuid}@loadtest.com",
            phone = $"+7-999-{Random.Shared.Next(100, 999)}-{Random.Shared.Next(1000, 9999)}",
            customFields = new Dictionary<string, string>
            {
                ["industry"] = Industries[Random.Shared.Next(Industries.Length)],
                ["forceScoringFail"] = "true"
            }
        };
    }

    public static object CreateDistributionFailureLead()
    {
        var company = Companies[Random.Shared.Next(Companies.Length)];
        var shortGuid = Guid.NewGuid().ToString("N")[..6];
        var emailGuid = Guid.NewGuid().ToString("N")[..8];

        return new
        {
            source = "load_test",
            companyName = $"{company} {shortGuid}",
            contactPerson = $"Test User {Random.Shared.Next(1000)}",
            email = $"test_{emailGuid}@loadtest.com",
            phone = $"+7-999-{Random.Shared.Next(100, 999)}-{Random.Shared.Next(1000, 9999)}",
            customFields = new Dictionary<string, string>
            {
                ["industry"] = Industries[Random.Shared.Next(Industries.Length)],
                ["forceDistributionFail"] = "true"
            }
        };
    }

    public static StringContent ToJsonContent(this object obj)
    {
        var json = JsonSerializer.Serialize(obj, JsonDefaults.Options);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}