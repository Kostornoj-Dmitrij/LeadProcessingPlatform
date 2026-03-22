using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnrichmentService.Domain.Entities;

namespace EnrichmentService.Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация EF Core для таблицы enrichment_results
/// </summary>
public class EnrichmentResultConfiguration : IEntityTypeConfiguration<EnrichmentResult>
{
    public void Configure(EntityTypeBuilder<EnrichmentResult> builder)
    {
        builder.ToTable("enrichment_results");
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

        builder.Property(x => x.Industry)
            .HasColumnName("industry")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.CompanySize)
            .HasColumnName("company_size")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Website)
            .HasColumnName("website")
            .HasMaxLength(255);

        builder.Property(x => x.RevenueRange)
            .HasColumnName("revenue_range")
            .HasMaxLength(50);

        builder.Property(x => x.RawResponse)
            .HasColumnName("raw_response")
            .HasColumnType("jsonb");

        builder.Property(x => x.EnrichedAt)
            .HasColumnName("enriched_at")
            .IsRequired();

        builder.HasIndex(x => x.LeadId)
            .HasDatabaseName("ix_enrichment_results_lead_id")
            .IsUnique();

        builder.HasIndex(x => x.EnrichedAt)
            .HasDatabaseName("ix_enrichment_results_enriched_at");
    }
}