using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScoringService.Domain.Constants;
using ScoringService.Domain.Entities;

namespace ScoringService.Infrastructure.Data;

/// <summary>
/// Инициализатор базы данных для заполнения начальными данными
/// </summary>
public static class DbInitializer
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Checking for existing scoring rules...");

        if (!await context.ScoringRules.AnyAsync(cancellationToken))
        {
            logger.LogInformation("No scoring rules found. Seeding initial rules...");

            var rules = new List<ScoringRule>
            {
                ScoringRule.Create(Guid.NewGuid(), "base_score", 
                    $"{{\"{RuleConfigKeys.Type}\": \"{RuleTypeConstants.AlwaysTrue}\"}}", 20),

                ScoringRule.Create(Guid.NewGuid(), "company_name_corp", 
                    $"{{\"{RuleConfigKeys.Type}\": \"{RuleTypeConstants.FieldContains}\", \"{RuleConfigKeys.Field}\": \"{RuleFieldConstants.CompanyName}\", \"{RuleConfigKeys.Value}\": \"Corp\"}}", 10, 1),

                ScoringRule.Create(Guid.NewGuid(), "company_name_inc", 
                    $"{{\"{RuleConfigKeys.Type}\": \"{RuleTypeConstants.FieldContains}\", \"{RuleConfigKeys.Field}\": \"{RuleFieldConstants.CompanyName}\", \"{RuleConfigKeys.Value}\": \"Inc\"}}", 10, 1),

                ScoringRule.Create(Guid.NewGuid(), "email_success_domain", 
                    $"{{\"{RuleConfigKeys.Type}\": \"{RuleTypeConstants.FieldContains}\", \"{RuleConfigKeys.Field}\": \"{RuleFieldConstants.Email}\", \"{RuleConfigKeys.Value}\": \"successcorp.com\"}}", 20, 2),

                ScoringRule.Create(Guid.NewGuid(), "industry_technology", 
                    $"{{\"{RuleConfigKeys.Type}\": \"{RuleTypeConstants.CustomFieldEquals}\", \"{RuleConfigKeys.FieldName}\": \"industry\", \"{RuleConfigKeys.Value}\": \"Technology\"}}", 30, 3),

                ScoringRule.Create(Guid.NewGuid(), "industry_healthcare", 
                    $"{{\"{RuleConfigKeys.Type}\": \"{RuleTypeConstants.CustomFieldEquals}\", \"{RuleConfigKeys.FieldName}\": \"industry\", \"{RuleConfigKeys.Value}\": \"Healthcare\"}}", 15, 3),

                ScoringRule.Create(Guid.NewGuid(), "industry_finance", 
                    $"{{\"{RuleConfigKeys.Type}\": \"{RuleTypeConstants.CustomFieldEquals}\", \"{RuleConfigKeys.FieldName}\": \"industry\", \"{RuleConfigKeys.Value}\": \"Finance\"}}", 25, 3),
            };

            await context.ScoringRules.AddRangeAsync(rules, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Successfully seeded {Count} scoring rules", rules.Count);
        }
        else
        {
            var count = await context.ScoringRules.CountAsync(cancellationToken);
            logger.LogInformation("Scoring rules already exist ({Count} rules found). Skipping seed.", count);
        }
    }
}