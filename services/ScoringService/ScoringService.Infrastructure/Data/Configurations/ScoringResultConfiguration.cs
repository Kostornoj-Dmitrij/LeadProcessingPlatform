using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoringService.Domain.Entities;

namespace ScoringService.Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация EF Core для таблицы scoring_results
/// </summary>
public class ScoringResultConfiguration : IEntityTypeConfiguration<ScoringResult>
{
    public void Configure(EntityTypeBuilder<ScoringResult> builder)
    {
        builder.ToTable("scoring_results");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.LeadId)
            .HasColumnName("lead_id")
            .IsRequired();

        builder.Property(x => x.TotalScore)
            .HasColumnName("total_score")
            .IsRequired();

        builder.Property(x => x.QualifiedThreshold)
            .HasColumnName("qualified_threshold")
            .IsRequired();

        builder.Property(x => x.AppliedRulesJson)
            .HasColumnName("applied_rules")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.CalculatedAt)
            .HasColumnName("calculated_at")
            .IsRequired();

        builder.HasIndex(x => x.LeadId)
            .IsUnique()
            .HasDatabaseName("ix_scoring_results_lead_id");
    }
}