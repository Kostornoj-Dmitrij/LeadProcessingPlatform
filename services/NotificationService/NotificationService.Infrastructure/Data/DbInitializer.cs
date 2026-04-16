using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NotificationService.Domain.Constants;
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
                    NotificationTypeConstants.LeadCreated,
                    NotificationChannel.Log,
                    $"[Lead Platform] Lead Created - {{{{{TemplateVariableKeys.CompanyName}}}}}",
                    $@"Lead created successfully.
                    Lead ID: {{{{{TemplateVariableKeys.LeadId}}}}}
                    Company: {{{{{TemplateVariableKeys.CompanyName}}}}}
                    Contact: {{{{{TemplateVariableKeys.ContactPerson}}}}}
                    Email: {{{{{TemplateVariableKeys.Email}}}}}
                    Source: {{{{{TemplateVariableKeys.Source}}}}}
                    Timestamp: {{{{{TemplateVariableKeys.Timestamp}}}}}",
                    [TemplateVariableKeys.LeadId, TemplateVariableKeys.CompanyName, TemplateVariableKeys.ContactPerson, 
                     TemplateVariableKeys.Email, TemplateVariableKeys.Source, TemplateVariableKeys.Timestamp]),

                NotificationTemplate.Create(
                    Guid.NewGuid(),
                    NotificationTypeConstants.LeadQualified,
                    NotificationChannel.Log,
                    $"[Lead Platform] Lead Qualified - {{{{{TemplateVariableKeys.CompanyName}}}}} (Score: {{{{{TemplateVariableKeys.Score}}}}})",
                    $@"Lead qualified successfully.
                    Lead ID: {{{{{TemplateVariableKeys.LeadId}}}}}
                    Company: {{{{{TemplateVariableKeys.CompanyName}}}}}
                    Contact: {{{{{TemplateVariableKeys.ContactPerson}}}}}
                    Email: {{{{{TemplateVariableKeys.Email}}}}}
                    Score: {{{{{TemplateVariableKeys.Score}}}}}
                    Industry: {{{{{TemplateVariableKeys.Industry}}}}}
                    Company Size: {{{{{TemplateVariableKeys.CompanySize}}}}}
                    Timestamp: {{{{{TemplateVariableKeys.Timestamp}}}}}",
                    [TemplateVariableKeys.LeadId, TemplateVariableKeys.CompanyName, TemplateVariableKeys.ContactPerson, 
                     TemplateVariableKeys.Email, TemplateVariableKeys.Score, TemplateVariableKeys.Industry, 
                     TemplateVariableKeys.CompanySize, TemplateVariableKeys.Timestamp]),

                NotificationTemplate.Create(
                    Guid.NewGuid(),
                    NotificationTypeConstants.LeadDistributed,
                    NotificationChannel.Log,
                    $"[Lead Platform] Lead Distributed - Lead {{{{{TemplateVariableKeys.LeadId}}}}}",
                    $@"Lead distributed successfully.
                    Lead ID: {{{{{TemplateVariableKeys.LeadId}}}}}
                    Target: {{{{{TemplateVariableKeys.Target}}}}}
                    Timestamp: {{{{{TemplateVariableKeys.Timestamp}}}}}",
                    [TemplateVariableKeys.LeadId, TemplateVariableKeys.Target, TemplateVariableKeys.Timestamp]),

                NotificationTemplate.Create(
                    Guid.NewGuid(),
                    NotificationTypeConstants.LeadDistributedFinal,
                    NotificationChannel.Log,
                    $"[Lead Platform] Lead Processing Completed (Distributed) - Lead {{{{{TemplateVariableKeys.LeadId}}}}}",
                    $@"Lead processing completed (Distributed - Closed).
                    Lead ID: {{{{{TemplateVariableKeys.LeadId}}}}}
                    Final Status: {{{{{TemplateVariableKeys.FinalStatus}}}}}
                    Timestamp: {{{{{TemplateVariableKeys.Timestamp}}}}}",
                    [TemplateVariableKeys.LeadId, TemplateVariableKeys.FinalStatus, TemplateVariableKeys.Timestamp]),

                NotificationTemplate.Create(
                    Guid.NewGuid(),
                    NotificationTypeConstants.LeadRejected,
                    NotificationChannel.Log,
                    $"[Lead Platform] Lead Rejected - Lead {{{{{TemplateVariableKeys.LeadId}}}}}",
                    $@"Lead rejected.
                    Lead ID: {{{{{TemplateVariableKeys.LeadId}}}}}
                    Reason: {{{{{TemplateVariableKeys.Reason}}}}}
                    Failure Type: {{{{{TemplateVariableKeys.FailureType}}}}}
                    Error Details: {{{{{TemplateVariableKeys.ErrorDetails}}}}}
                    Timestamp: {{{{{TemplateVariableKeys.Timestamp}}}}}",
                    [TemplateVariableKeys.LeadId, TemplateVariableKeys.Reason, TemplateVariableKeys.FailureType, 
                     TemplateVariableKeys.ErrorDetails, TemplateVariableKeys.Timestamp]),

                NotificationTemplate.Create(
                    Guid.NewGuid(),
                    NotificationTypeConstants.LeadRejectedFinal,
                    NotificationChannel.Log,
                    $"[Lead Platform] Lead Processing Completed (Rejected) - Lead {{{{{TemplateVariableKeys.LeadId}}}}}",
                    $@"Lead processing completed (Rejected - Closed).
                    Lead ID: {{{{{TemplateVariableKeys.LeadId}}}}}
                    Final Status: {{{{{TemplateVariableKeys.FinalStatus}}}}}
                    Timestamp: {{{{{TemplateVariableKeys.Timestamp}}}}}",
                    [TemplateVariableKeys.LeadId, TemplateVariableKeys.FinalStatus, TemplateVariableKeys.Timestamp]),

                NotificationTemplate.Create(
                    Guid.NewGuid(),
                    NotificationTypeConstants.LeadDistributionFailed,
                    NotificationChannel.Log,
                    $"[Lead Platform] Lead Distribution Failed - Lead {{{{{TemplateVariableKeys.LeadId}}}}}",
                    $@"Lead distribution failed.
                    Lead ID: {{{{{TemplateVariableKeys.LeadId}}}}}
                    Reason: {{{{{TemplateVariableKeys.Reason}}}}}
                    Timestamp: {{{{{TemplateVariableKeys.Timestamp}}}}}",
                    [TemplateVariableKeys.LeadId, TemplateVariableKeys.Reason, TemplateVariableKeys.Timestamp]),

                NotificationTemplate.Create(
                    Guid.NewGuid(),
                    NotificationTypeConstants.LeadDistributionFailedFinal,
                    NotificationChannel.Log,
                    $"[Lead Platform] Lead Processing Completed (Distribution Failed) - Lead {{{{{TemplateVariableKeys.LeadId}}}}}",
                    $@"Lead processing completed (Distribution Failed - Closed).
                    Lead ID: {{{{{TemplateVariableKeys.LeadId}}}}}
                    Final Status: {{{{{TemplateVariableKeys.FinalStatus}}}}}
                    Timestamp: {{{{{TemplateVariableKeys.Timestamp}}}}}",
                    [TemplateVariableKeys.LeadId, TemplateVariableKeys.FinalStatus, TemplateVariableKeys.Timestamp]),

                NotificationTemplate.Create(
                    Guid.NewGuid(),
                    NotificationTypeConstants.LeadCreated,
                    NotificationChannel.Email,
                    $"[Lead Platform] Lead Created - {{{{{TemplateVariableKeys.CompanyName}}}}}",
                    $@"Lead ID: {{{{{TemplateVariableKeys.LeadId}}}}}
                    Company: {{{{{TemplateVariableKeys.CompanyName}}}}}
                    Contact: {{{{{TemplateVariableKeys.ContactPerson}}}}}
                    Email: {{{{{TemplateVariableKeys.Email}}}}}
                    Source: {{{{{TemplateVariableKeys.Source}}}}}
                    Timestamp: {{{{{TemplateVariableKeys.Timestamp}}}}}",
                    [TemplateVariableKeys.LeadId, TemplateVariableKeys.CompanyName, TemplateVariableKeys.ContactPerson,
                     TemplateVariableKeys.Email, TemplateVariableKeys.Source, TemplateVariableKeys.Timestamp]),

                NotificationTemplate.Create(
                    Guid.NewGuid(),
                    NotificationTypeConstants.LeadQualified,
                    NotificationChannel.Email,
                    $"[Lead Platform] Lead Qualified - {{{{{TemplateVariableKeys.CompanyName}}}}} (Score: {{{{{TemplateVariableKeys.Score}}}}})",
                    $@"Lead ID: {{{{{TemplateVariableKeys.LeadId}}}}}
                    Company: {{{{{TemplateVariableKeys.CompanyName}}}}}
                    Contact: {{{{{TemplateVariableKeys.ContactPerson}}}}}
                    Email: {{{{{TemplateVariableKeys.Email}}}}}
                    Score: {{{{{TemplateVariableKeys.Score}}}}}
                    Industry: {{{{{TemplateVariableKeys.Industry}}}}}
                    Company Size: {{{{{TemplateVariableKeys.CompanySize}}}}}
                    Timestamp: {{{{{TemplateVariableKeys.Timestamp}}}}}",
                    [TemplateVariableKeys.LeadId, TemplateVariableKeys.CompanyName, TemplateVariableKeys.ContactPerson,
                     TemplateVariableKeys.Email, TemplateVariableKeys.Score, TemplateVariableKeys.Industry,
                     TemplateVariableKeys.CompanySize, TemplateVariableKeys.Timestamp]),

                NotificationTemplate.Create(
                    Guid.NewGuid(),
                    NotificationTypeConstants.LeadDistributed,
                    NotificationChannel.Email,
                    $"[Lead Platform] Lead Distributed - Lead {{{{{TemplateVariableKeys.LeadId}}}}}",
                    $@"Lead ID: {{{{{TemplateVariableKeys.LeadId}}}}}
                    Target: {{{{{TemplateVariableKeys.Target}}}}}
                    Timestamp: {{{{{TemplateVariableKeys.Timestamp}}}}}",
                    [TemplateVariableKeys.LeadId, TemplateVariableKeys.Target, TemplateVariableKeys.Timestamp]),

                NotificationTemplate.Create(
                    Guid.NewGuid(),
                    NotificationTypeConstants.LeadDistributedFinal,
                    NotificationChannel.Email,
                    $"[Lead Platform] Lead Processing Completed (Distributed) - Lead {{{{{TemplateVariableKeys.LeadId}}}}}",
                    $@"Lead ID: {{{{{TemplateVariableKeys.LeadId}}}}}
                    Final Status: {{{{{TemplateVariableKeys.FinalStatus}}}}}
                    Timestamp: {{{{{TemplateVariableKeys.Timestamp}}}}}",
                    [TemplateVariableKeys.LeadId, TemplateVariableKeys.FinalStatus, TemplateVariableKeys.Timestamp]),

                NotificationTemplate.Create(
                    Guid.NewGuid(),
                    NotificationTypeConstants.LeadRejected,
                    NotificationChannel.Email,
                    $"[Lead Platform] Lead Rejected - Lead {{{{{TemplateVariableKeys.LeadId}}}}}",
                    $@"Lead ID: {{{{{TemplateVariableKeys.LeadId}}}}}
                    Reason: {{{{{TemplateVariableKeys.Reason}}}}}
                    Failure Type: {{{{{TemplateVariableKeys.FailureType}}}}}
                    Error Details: {{{{{TemplateVariableKeys.ErrorDetails}}}}}
                    Timestamp: {{{{{TemplateVariableKeys.Timestamp}}}}}",
                    [TemplateVariableKeys.LeadId, TemplateVariableKeys.Reason, TemplateVariableKeys.FailureType,
                     TemplateVariableKeys.ErrorDetails, TemplateVariableKeys.Timestamp]),

                NotificationTemplate.Create(
                    Guid.NewGuid(),
                    NotificationTypeConstants.LeadRejectedFinal,
                    NotificationChannel.Email,
                    $"[Lead Platform] Lead Processing Completed (Rejected) - Lead {{{{{TemplateVariableKeys.LeadId}}}}}",
                    $@"Lead ID: {{{{{TemplateVariableKeys.LeadId}}}}}
                    Final Status: {{{{{TemplateVariableKeys.FinalStatus}}}}}
                    Timestamp: {{{{{TemplateVariableKeys.Timestamp}}}}}",
                    [TemplateVariableKeys.LeadId, TemplateVariableKeys.FinalStatus, TemplateVariableKeys.Timestamp]),

                NotificationTemplate.Create(
                    Guid.NewGuid(),
                    NotificationTypeConstants.LeadDistributionFailed,
                    NotificationChannel.Email,
                    $"[Lead Platform] Lead Distribution Failed - Lead {{{{{TemplateVariableKeys.LeadId}}}}}",
                    $@"Lead ID: {{{{{TemplateVariableKeys.LeadId}}}}}
                    Reason: {{{{{TemplateVariableKeys.Reason}}}}}
                    Timestamp: {{{{{TemplateVariableKeys.Timestamp}}}}}",
                    [TemplateVariableKeys.LeadId, TemplateVariableKeys.Reason, TemplateVariableKeys.Timestamp]),

                NotificationTemplate.Create(
                    Guid.NewGuid(),
                    NotificationTypeConstants.LeadDistributionFailedFinal,
                    NotificationChannel.Email,
                    $"[Lead Platform] Lead Processing Completed (Distribution Failed) - Lead {{{{{TemplateVariableKeys.LeadId}}}}}",
                    $@"Lead ID: {{{{{TemplateVariableKeys.LeadId}}}}}
                    Final Status: {{{{{TemplateVariableKeys.FinalStatus}}}}}
                    Timestamp: {{{{{TemplateVariableKeys.Timestamp}}}}}",
                    [TemplateVariableKeys.LeadId, TemplateVariableKeys.FinalStatus, TemplateVariableKeys.Timestamp])
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