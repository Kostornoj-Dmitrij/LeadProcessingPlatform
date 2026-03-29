using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DistributionService.Domain.Entities;

namespace DistributionService.Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация EF Core для таблицы distribution_rules
/// </summary>
public class DistributionRuleConfiguration : IEntityTypeConfiguration<DistributionRule>
{
    public void Configure(EntityTypeBuilder<DistributionRule> builder)
    {
        builder.ToTable("distribution_rules");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.RuleName)
            .HasColumnName("rule_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description");

        builder.Property(x => x.Strategy)
            .HasColumnName("strategy")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.ConditionJson)
            .HasColumnName("condition")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.TargetConfigJson)
            .HasColumnName("target_config")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.Priority)
            .HasColumnName("priority")
            .HasDefaultValue(0);

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(x => x.ValidFrom)
            .HasColumnName("valid_from")
            .IsRequired();

        builder.Property(x => x.ValidTo)
            .HasColumnName("valid_to");

        builder.Property(x => x.Version)
            .HasColumnName("version")
            .HasDefaultValue(1);

        builder.HasIndex(x => x.RuleName)
            .IsUnique()
            .HasDatabaseName("ix_distribution_rules_rule_name");

        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("ix_distribution_rules_is_active");
    }
}