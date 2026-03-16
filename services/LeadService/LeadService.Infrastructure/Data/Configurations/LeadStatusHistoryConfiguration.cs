using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LeadService.Infrastructure.Data.Entities;

namespace LeadService.Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация EF Core для таблицы LeadStatusHistory
/// </summary>
public class LeadStatusHistoryConfiguration : IEntityTypeConfiguration<LeadStatusHistory>
{
    public void Configure(EntityTypeBuilder<LeadStatusHistory> builder)
    {
        builder.ToTable("LeadStatusHistory");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.LeadId)
            .HasColumnName("LeadId")
            .IsRequired();
        
        builder.Property(x => x.OldStatus)
            .HasColumnName("OldStatus")
            .HasMaxLength(50);
        
        builder.Property(x => x.NewStatus)
            .HasColumnName("NewStatus")
            .HasMaxLength(50)
            .IsRequired();
        
        builder.Property(x => x.ChangedAt)
            .HasColumnName("ChangedAt")
            .IsRequired();
        
        builder.Property(x => x.Reason)
            .HasColumnName("Reason");
        
        builder.Property(x => x.EventId)
            .HasColumnName("EventId");
        
        builder.HasIndex(x => x.LeadId)
            .HasDatabaseName("IX_LeadStatusHistory_LeadId");
        
        builder.HasIndex(x => x.ChangedAt)
            .HasDatabaseName("IX_LeadStatusHistory_ChangedAt");
        
        builder.HasOne(x => x.Lead)
            .WithMany()
            .HasForeignKey(x => x.LeadId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}