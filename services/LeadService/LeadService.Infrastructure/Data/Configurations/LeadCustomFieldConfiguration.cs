using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LeadService.Domain.Entities;

namespace LeadService.Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация EF Core для таблицы lead_custom_fields
/// </summary>
public class LeadCustomFieldConfiguration : IEntityTypeConfiguration<LeadCustomField>
{
    public void Configure(EntityTypeBuilder<LeadCustomField> builder)
    {
        builder.ToTable("lead_custom_fields");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.LeadId)
            .HasColumnName("lead_id")
            .IsRequired();

        builder.Property(x => x.FieldName)
            .HasColumnName("field_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.FieldValue)
            .HasColumnName("field_value")
            .IsRequired();

        builder.HasIndex(x => new { x.LeadId, x.FieldName })
            .IsUnique()
            .HasDatabaseName("ix_lead_custom_fields_lead_id_field_name");
    }
}