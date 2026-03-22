using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LeadService.Domain.Entities;
using LeadService.Domain.ValueObjects;

namespace LeadService.Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация EF Core для таблицы leads
/// </summary>
public class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.ToTable("leads");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CompanyName)
            .HasConversion(
                v => v.Value,
                v => CompanyName.CreateUnsafe(v))
            .HasColumnName("company_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Email)
            .HasConversion(
                v => v.Value,
                v => Email.CreateUnsafe(v))
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Phone)
            .HasConversion(
                v => v != null ? v.Value : null,
                v => v != null ? Phone.CreateUnsafe(v) : null)
            .HasColumnName("phone")
            .HasMaxLength(50);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.ExternalLeadId)
            .HasColumnName("external_lead_id")
            .HasMaxLength(255);

        builder.Property(x => x.Source)
            .HasColumnName("source")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.ContactPerson)
            .HasColumnName("contact_person")
            .HasMaxLength(255);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Score)
            .HasColumnName("score");

        builder.Property(x => x.IsEnrichmentCompensated)
            .HasColumnName("is_enrichment_compensated")
            .HasDefaultValue(false);

        builder.Property(x => x.IsScoringCompensated)
            .HasColumnName("is_scoring_compensated")
            .HasDefaultValue(false);

        builder.Property(x => x.IsEnrichmentReceived)
            .HasColumnName("is_enrichment_received")
            .HasDefaultValue(false);

        builder.Property(x => x.IsScoringReceived)
            .HasColumnName("is_scoring_received")
            .HasDefaultValue(false);

        builder.Property(x => x.EnrichedData)
            .HasColumnName("enriched_data")
            .HasColumnType("jsonb");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(x => x.ExternalLeadId)
            .IsUnique()
            .HasDatabaseName("ix_leads_external_lead_id")
            .HasFilter("external_lead_id IS NOT NULL");

        builder.HasIndex(x => x.Email)
            .HasDatabaseName("ix_leads_email");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("ix_leads_status");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("ix_leads_created_at");

        builder.HasMany(x => x.CustomFields)
            .WithOne()
            .HasForeignKey(x => x.LeadId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}