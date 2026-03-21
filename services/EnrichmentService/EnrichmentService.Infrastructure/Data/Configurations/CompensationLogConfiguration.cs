using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnrichmentService.Domain.Entities;

namespace EnrichmentService.Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация EF Core для таблицы compensation_logs
/// </summary>
public class CompensationLogConfiguration : IEntityTypeConfiguration<CompensationLog>
{
    public void Configure(EntityTypeBuilder<CompensationLog> builder)
    {
        builder.ToTable("compensation_logs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.LeadId)
            .HasColumnName("lead_id")
            .IsRequired();

        builder.Property(x => x.CompensationType)
            .HasColumnName("compensation_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasColumnName("reason");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.ProcessedAt)
            .HasColumnName("processed_at");

        builder.Property(x => x.IsCompensated)
            .HasColumnName("is_compensated")
            .HasDefaultValue(false);

        builder.HasIndex(x => x.LeadId)
            .HasDatabaseName("ix_compensation_logs_lead_id");
    }
}