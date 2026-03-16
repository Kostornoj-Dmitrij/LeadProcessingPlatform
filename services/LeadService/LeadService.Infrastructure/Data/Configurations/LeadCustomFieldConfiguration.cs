using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LeadService.Domain.Entities;

namespace LeadService.Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация EF Core для таблицы LeadCustomFields
/// </summary>
public class LeadCustomFieldConfiguration : IEntityTypeConfiguration<LeadCustomField>
{
    public void Configure(EntityTypeBuilder<LeadCustomField> builder)
    {
        builder.ToTable("LeadCustomFields");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.LeadId)
            .HasColumnName("LeadId")
            .IsRequired();
        
        builder.Property(x => x.FieldName)
            .HasColumnName("FieldName")
            .HasMaxLength(100)
            .IsRequired();
        
        builder.Property(x => x.FieldValue)
            .HasColumnName("FieldValue")
            .IsRequired();
        
        builder.HasIndex(x => new { x.LeadId, x.FieldName })
            .IsUnique()
            .HasDatabaseName("IX_LeadCustomFields_LeadId_FieldName");
    }
}