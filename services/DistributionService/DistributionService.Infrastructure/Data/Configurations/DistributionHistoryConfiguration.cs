using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DistributionService.Domain.Entities;

namespace DistributionService.Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация EF Core для таблицы distribution_history
/// </summary>
public class DistributionHistoryConfiguration : IEntityTypeConfiguration<DistributionHistory>
{
    public void Configure(EntityTypeBuilder<DistributionHistory> builder)
    {
        builder.ToTable("distribution_history");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.LeadId)
            .HasColumnName("lead_id")
            .IsRequired();

        builder.Property(x => x.RuleId)
            .HasColumnName("rule_id");

        builder.Property(x => x.Target)
            .HasColumnName("target")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.ResponseData)
            .HasColumnName("response_data")
            .HasColumnType("jsonb");

        builder.Property(x => x.ErrorMessage)
            .HasColumnName("error_message");

        builder.Property(x => x.DistributedAt)
            .HasColumnName("distributed_at")
            .IsRequired();

        builder.HasIndex(x => x.LeadId)
            .HasDatabaseName("ix_distribution_history_lead_id");

        builder.HasIndex(x => x.DistributedAt)
            .HasDatabaseName("ix_distribution_history_distributed_at");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("ix_distribution_history_status");
    }
}