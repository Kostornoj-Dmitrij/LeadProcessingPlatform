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
        builder.ToTable("lead_status_history");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.LeadId)
            .HasColumnName("lead_id")
            .IsRequired();
        
        builder.Property(x => x.OldStatus)
            .HasColumnName("old_status")
            .HasMaxLength(50);
        
        builder.Property(x => x.NewStatus)
            .HasColumnName("new_status")
            .HasMaxLength(50)
            .IsRequired();
        
        builder.Property(x => x.ChangedAt)
            .HasColumnName("changed_at")
            .IsRequired();
        
        builder.Property(x => x.Reason)
            .HasColumnName("reason");
        
        builder.Property(x => x.EventId)
            .HasColumnName("event_id");
        
        builder.HasIndex(x => x.LeadId)
            .HasDatabaseName("ix_lead_status_history_lead_id");
        
        builder.HasIndex(x => x.ChangedAt)
            .HasDatabaseName("ix_lead_status_history_changed_at");
        
        builder.HasOne(x => x.Lead)
            .WithMany()
            .HasForeignKey(x => x.LeadId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}