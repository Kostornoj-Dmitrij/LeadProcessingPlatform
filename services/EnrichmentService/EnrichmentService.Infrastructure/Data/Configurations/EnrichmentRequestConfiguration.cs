using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnrichmentService.Domain.Entities;

namespace EnrichmentService.Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация EF Core для таблицы enrichment_requests
/// </summary>
public class EnrichmentRequestConfiguration : IEntityTypeConfiguration<EnrichmentRequest>
{
    public void Configure(EntityTypeBuilder<EnrichmentRequest> builder)
    {
        builder.ToTable("enrichment_requests");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.LeadId)
            .HasColumnName("lead_id")
            .IsRequired();

        builder.Property(x => x.CompanyName)
            .HasColumnName("company_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.ContactPerson)
            .HasColumnName("contact_person")
            .HasMaxLength(255);

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.CustomFields)
            .HasColumnName("custom_fields")
            .HasColumnType("jsonb");

        builder.Property(x => x.TraceParent)
            .HasColumnName("trace_parent")
            .HasMaxLength(255);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.RetryCount)
            .HasColumnName("retry_count")
            .HasDefaultValue(0);

        builder.Property(x => x.LastAttemptAt)
            .HasColumnName("last_attempt_at");

        builder.Property(x => x.ErrorMessage)
            .HasColumnName("error_message");

        builder.Property(x => x.NextRetryAt)
            .HasColumnName("next_retry_at");

        builder.HasIndex(x => x.LeadId)
            .IsUnique()
            .HasDatabaseName("ix_enrichment_requests_lead_id");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("ix_enrichment_requests_status");

        builder.HasIndex(x => new { x.Status, x.NextRetryAt })
            .HasDatabaseName("ix_enrichment_requests_status_next_retry_at");
    }
}