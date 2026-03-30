using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;

namespace NotificationService.Infrastructure.Data;

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
        logger.LogInformation("Checking for existing notification templates...");

        if (!await context.NotificationTemplates.AnyAsync(cancellationToken))
        {
            logger.LogInformation("No notification templates found. Seeding initial templates...");

            var templates = new List<NotificationTemplate>
            {
                NotificationTemplate.Create(
                    Guid.NewGuid(),
                    "LeadCreated",
                    NotificationChannel.Log,
                    "[Lead Platform] Lead Created - {{CompanyName}}",
                    @"Lead created successfully.
                    Lead ID: {{LeadId}}
                    Company: {{CompanyName}}
                    Contact: {{ContactPerson}}
                    Email: {{Email}}
                    Source: {{Source}}
                    Timestamp: {{Timestamp}}",
                    ["LeadId", "CompanyName", "ContactPerson", "Email", "Source", "Timestamp"]),

                NotificationTemplate.Create(
                    Guid.NewGuid(),
                    "LeadQualified",
                    NotificationChannel.Log,
                    "[Lead Platform] Lead Qualified - {{CompanyName}} (Score: {{Score}})",
                    @"Lead qualified successfully.
                    Lead ID: {{LeadId}}
                    Company: {{CompanyName}}
                    Contact: {{ContactPerson}}
                    Email: {{Email}}
                    Score: {{Score}}
                    Industry: {{Industry}}
                    Company Size: {{CompanySize}}
                    Timestamp: {{Timestamp}}",
                    ["LeadId", "CompanyName", "ContactPerson", "Email", "Score", "Industry", "CompanySize", "Timestamp"]),

                NotificationTemplate.Create(
                    Guid.NewGuid(),
                    "LeadDistributed",
                    NotificationChannel.Log,
                    "[Lead Platform] Lead Distributed - Lead {{LeadId}}",
                    @"Lead distributed successfully.
                    Lead ID: {{LeadId}}
                    Target: {{Target}}
                    Timestamp: {{Timestamp}}",
                    ["LeadId", "Target", "Timestamp"]),

                NotificationTemplate.Create(
                    Guid.NewGuid(),
                    "LeadDistributedFinal",
                    NotificationChannel.Log,
                    "[Lead Platform] Lead Processing Completed (Distributed) - Lead {{LeadId}}",
                    @"Lead processing completed (Distributed - Closed).
                    Lead ID: {{LeadId}}
                    Final Status: {{FinalStatus}}
                    Timestamp: {{Timestamp}}",
                    ["LeadId", "FinalStatus", "Timestamp"]),

                NotificationTemplate.Create(
                    Guid.NewGuid(),
                    "LeadRejected",
                    NotificationChannel.Log,
                    "[Lead Platform] Lead Rejected - Lead {{LeadId}}",
                    @"Lead rejected.
                    Lead ID: {{LeadId}}
                    Reason: {{Reason}}
                    Failure Type: {{FailureType}}
                    Error Details: {{ErrorDetails}}
                    Timestamp: {{Timestamp}}",
                    ["LeadId", "Reason", "FailureType", "ErrorDetails", "Timestamp"]),

                NotificationTemplate.Create(
                    Guid.NewGuid(),
                    "LeadRejectedFinal",
                    NotificationChannel.Log,
                    "[Lead Platform] Lead Processing Completed (Rejected) - Lead {{LeadId}}",
                    @"Lead processing completed (Rejected - Closed).
                    Lead ID: {{LeadId}}
                    Final Status: {{FinalStatus}}
                    Timestamp: {{Timestamp}}",
                    ["LeadId", "FinalStatus", "Timestamp"]),

                NotificationTemplate.Create(
                    Guid.NewGuid(),
                    "LeadDistributionFailed",
                    NotificationChannel.Log,
                    "[Lead Platform] Lead Distribution Failed - Lead {{LeadId}}",
                    @"Lead distribution failed.
                    Lead ID: {{LeadId}}
                    Reason: {{Reason}}
                    Timestamp: {{Timestamp}}",
                    ["LeadId", "Reason", "Timestamp"]),

                NotificationTemplate.Create(
                    Guid.NewGuid(),
                    "LeadDistributionFailedFinal",
                    NotificationChannel.Log,
                    "[Lead Platform] Lead Processing Completed (Distribution Failed) - Lead {{LeadId}}",
                    @"Lead processing completed (Distribution Failed - Closed).
                    Lead ID: {{LeadId}}
                    Final Status: {{FinalStatus}}
                    Timestamp: {{Timestamp}}",
                    ["LeadId", "FinalStatus", "Timestamp"]),

                NotificationTemplate.Create(
                    Guid.NewGuid(),
                    "LeadCreated",
                    NotificationChannel.Email,
                    "[Lead Platform] Lead Created - {{CompanyName}}",
                    @"Lead ID: {{LeadId}}
                    Company: {{CompanyName}}
                    Contact: {{ContactPerson}}
                    Email: {{Email}}
                    Source: {{Source}}
                    Timestamp: {{Timestamp}}",
                    ["LeadId", "CompanyName", "ContactPerson", "Email", "Source", "Timestamp"]),

                NotificationTemplate.Create(
                    Guid.NewGuid(),
                    "LeadQualified",
                    NotificationChannel.Email,
                    "[Lead Platform] Lead Qualified - {{CompanyName}} (Score: {{Score}})",
                    @"Lead ID: {{LeadId}}
                    Company: {{CompanyName}}
                    Contact: {{ContactPerson}}
                    Email: {{Email}}
                    Score: {{Score}}
                    Industry: {{Industry}}
                    Company Size: {{CompanySize}}
                    Timestamp: {{Timestamp}}",
                    ["LeadId", "CompanyName", "ContactPerson", "Email", "Score", "Industry", "CompanySize", "Timestamp"]),

                NotificationTemplate.Create(
                    Guid.NewGuid(),
                    "LeadDistributed",
                    NotificationChannel.Email,
                    "[Lead Platform] Lead Distributed - Lead {{LeadId}}",
                    @"Lead ID: {{LeadId}}
                    Target: {{Target}}
                    Timestamp: {{Timestamp}}",
                    ["LeadId", "Target", "Timestamp"]),

                NotificationTemplate.Create(
                    Guid.NewGuid(),
                    "LeadDistributedFinal",
                    NotificationChannel.Email,
                    "[Lead Platform] Lead Processing Completed (Distributed) - Lead {{LeadId}}",
                    @"Lead ID: {{LeadId}}
                    Final Status: {{FinalStatus}}
                    Timestamp: {{Timestamp}}",
                    ["LeadId", "FinalStatus", "Timestamp"]),

                NotificationTemplate.Create(
                    Guid.NewGuid(),
                    "LeadRejected",
                    NotificationChannel.Email,
                    "[Lead Platform] Lead Rejected - Lead {{LeadId}}",
                    @"Lead ID: {{LeadId}}
                    Reason: {{Reason}}
                    Failure Type: {{FailureType}}
                    Error Details: {{ErrorDetails}}
                    Timestamp: {{Timestamp}}",
                    ["LeadId", "Reason", "FailureType", "ErrorDetails", "Timestamp"]),

                NotificationTemplate.Create(
                    Guid.NewGuid(),
                    "LeadRejectedFinal",
                    NotificationChannel.Email,
                    "[Lead Platform] Lead Processing Completed (Rejected) - Lead {{LeadId}}",
                    @"Lead ID: {{LeadId}}
                    Final Status: {{FinalStatus}}
                    Timestamp: {{Timestamp}}",
                    ["LeadId", "FinalStatus", "Timestamp"]),

                NotificationTemplate.Create(
                    Guid.NewGuid(),
                    "LeadDistributionFailed",
                    NotificationChannel.Email,
                    "[Lead Platform] Lead Distribution Failed - Lead {{LeadId}}",
                    @"Lead ID: {{LeadId}}
                    Reason: {{Reason}}
                    Timestamp: {{Timestamp}}",
                    ["LeadId", "Reason", "Timestamp"]),

                NotificationTemplate.Create(
                    Guid.NewGuid(),
                    "LeadDistributionFailedFinal",
                    NotificationChannel.Email,
                    "[Lead Platform] Lead Processing Completed (Distribution Failed) - Lead {{LeadId}}",
                    @"Lead ID: {{LeadId}}
                    Final Status: {{FinalStatus}}
                    Timestamp: {{Timestamp}}",
                    ["LeadId", "FinalStatus", "Timestamp"])
            };

            await context.NotificationTemplates.AddRangeAsync(templates, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            
            logger.LogInformation("Successfully seeded {Count} notification templates", templates.Count);
        }
        else
        {
            var count = await context.NotificationTemplates.CountAsync(cancellationToken);
            logger.LogInformation("Notification templates already exist ({Count} templates found). Skipping seed.", count);
        }
    }
}