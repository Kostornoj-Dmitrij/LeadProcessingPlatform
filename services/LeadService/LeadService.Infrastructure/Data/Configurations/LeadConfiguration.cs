using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LeadService.Domain.Entities;
using LeadService.Domain.ValueObjects;

namespace LeadService.Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация EF Core для таблицы Leads
/// </summary>
public class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.ToTable("Leads");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.CompanyName)
            .HasConversion(
                v => v.Value,
                v => CompanyName.CreateUnsafe(v))
            .HasColumnName("CompanyName")
            .HasMaxLength(255)
            .IsRequired();
        
        builder.Property(x => x.Email)
            .HasConversion(
                v => v.Value,
                v => Email.CreateUnsafe(v))
            .HasColumnName("Email")
            .HasMaxLength(255)
            .IsRequired();
        
        builder.Property(x => x.Phone)
            .HasConversion(
                v => v != null ? v.Value : null,
                v => v != null ? Phone.CreateUnsafe(v) : null)
            .HasColumnName("Phone")
            .HasMaxLength(50);
        
        builder.Property(x => x.Id)
            .HasColumnName("Id")
            .IsRequired();
        
        builder.Property(x => x.ExternalLeadId)
            .HasColumnName("ExternalLeadId")
            .HasMaxLength(255);
        
        builder.Property(x => x.Source)
            .HasColumnName("Source")
            .HasMaxLength(100)
            .IsRequired();
        
        builder.Property(x => x.ContactPerson)
            .HasColumnName("ContactPerson")
            .HasMaxLength(255);
        
        builder.Property(x => x.Status)
            .HasColumnName("Status")
            .HasConversion<int>()
            .IsRequired();
        
        builder.Property(x => x.Score)
            .HasColumnName("Score");

        builder.Property(x => x.IsEnrichmentCompensated)
            .HasColumnName("IsEnrichmentCompensated")
            .HasDefaultValue(false);

        builder.Property(x => x.IsScoringCompensated)
            .HasColumnName("IsScoringCompensated")
            .HasDefaultValue(false);

        builder.Property(x => x.IsEnrichmentReceived)
            .HasColumnName("IsEnrichmentReceived")
            .HasDefaultValue(false);

        builder.Property(x => x.IsScoringReceived)
            .HasColumnName("IsScoringReceived")
            .HasDefaultValue(false);

        builder.Property(x => x.EnrichedData)
            .HasColumnName("EnrichedData")
            .HasColumnType("jsonb");
        
        builder.Property(x => x.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();
        
        builder.Property(x => x.UpdatedAt)
            .HasColumnName("UpdatedAt")
            .IsRequired();

        builder.Property(x => x.Version)
            .HasColumnName("xmin")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
        
        builder.HasIndex(x => x.ExternalLeadId)
            .IsUnique()
            .HasDatabaseName("IX_Leads_ExternalLeadId")
            .HasFilter("ExternalLeadId IS NOT NULL");
        
        builder.HasIndex(x => x.Email)
            .HasDatabaseName("IX_Leads_Email");
        
        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_Leads_Status");
        
        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("IX_Leads_CreatedAt");
        
        builder.HasMany(x => x.CustomFields)
            .WithOne()
            .HasForeignKey(x => x.LeadId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}