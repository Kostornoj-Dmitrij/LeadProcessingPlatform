using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DistributionService.Domain.Entities;
using DistributionService.Domain.Enums;

namespace DistributionService.Infrastructure.Data;

/// <summary>
/// Инициализатор базы данных с начальными правилами распределения
/// </summary>
public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.DistributionRules.AnyAsync())
        {
            logger.LogInformation("Distribution rules already exist, skipping seed");
            return;
        }

        logger.LogInformation("Seeding initial distribution rules...");

        var rules = new List<DistributionRule>
        {
            DistributionRule.Create(
                Guid.NewGuid(),
                "High Score Enterprise",
                DistributionRuleStrategy.ScoreBased,
                "{\"type\": \"score_threshold\", \"min_score\": 80}",
                "{\"thresholds\": [{\"min_score\": 90, \"target\": \"enterprise_sales\"}, {\"min_score\": 80, \"target\": \"mid_market_sales\"}], \"default_target\": \"standard_sales\"}",
                10),

            DistributionRule.Create(
                Guid.NewGuid(),
                "Technology Industry",
                DistributionRuleStrategy.Territory,
                "{\"type\": \"industry_match\", \"industry\": \"Technology\"}",
                "{\"territories\": {\"technology\": \"tech_specialists\", \"finance\": \"finance_team\", \"default\": \"general_sales\"}}",
                20),

            DistributionRule.Create(
                Guid.NewGuid(),
                "Round Robin Default",
                DistributionRuleStrategy.RoundRobin,
                "{\"type\": \"always_true\"}",
                "{\"targets\": [\"sales_rep_1\", \"sales_rep_2\", \"sales_rep_3\", \"sales_rep_4\"]}",
                30)
        };

        await context.DistributionRules.AddRangeAsync(rules);
        await context.SaveChangesAsync();

        logger.LogInformation("Seeded {Count} distribution rules", rules.Count);
    }
}