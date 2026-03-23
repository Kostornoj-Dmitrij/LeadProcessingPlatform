using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoringService.Domain.Entities;

namespace ScoringService.Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация EF Core для таблицы pending_enriched_data
/// </summary>
public class PendingEnrichedDataConfiguration : IEntityTypeConfiguration<PendingEnrichedData>
{
    public void Configure(EntityTypeBuilder<PendingEnrichedData> builder)
    {
        builder.ToTable("pending_enriched_data");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.LeadId)
            .HasColumnName("lead_id")
            .IsRequired();

        builder.Property(x => x.EnrichedDataJson)
            .HasColumnName("enriched_data_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.ReceivedAt)
            .HasColumnName("received_at")
            .IsRequired();

        builder.Property(x => x.IsProcessed)
            .HasColumnName("is_processed")
            .HasDefaultValue(false);

        builder.HasIndex(x => x.LeadId)
            .HasDatabaseName("ix_pending_enriched_data_lead_id");

        builder.HasIndex(x => new { x.IsProcessed, x.ReceivedAt })
            .HasDatabaseName("ix_pending_enriched_data_is_processed_received_at");
    }
}