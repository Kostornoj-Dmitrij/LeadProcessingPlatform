using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedKernel.Entities;

namespace LeadService.Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация EF Core для таблицы OutboxMessages
/// </summary>
public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.AggregateType)
            .HasColumnName("AggregateType")
            .HasMaxLength(100)
            .IsRequired();
        
        builder.Property(x => x.AggregateId)
            .HasColumnName("AggregateId")
            .HasMaxLength(255)
            .IsRequired();
        
        builder.Property(x => x.EventType)
            .HasColumnName("EventType")
            .HasMaxLength(100)
            .IsRequired();
        
        builder.Property(x => x.Payload)
            .HasColumnName("Payload")
            .HasColumnType("jsonb")
            .IsRequired();
        
        builder.Property(x => x.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();
        
        builder.Property(x => x.ProcessedAt)
            .HasColumnName("ProcessedAt");
        
        builder.Property(x => x.ProcessingAttempts)
            .HasColumnName("ProcessingAttempts")
            .HasDefaultValue(0);
        
        builder.Property(x => x.ErrorMessage)
            .HasColumnName("ErrorMessage");
        
        builder.HasIndex(x => x.ProcessedAt)
            .HasDatabaseName("IX_OutboxMessages_ProcessedAt");
    }
}