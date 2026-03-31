using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedKernel.Entities;

namespace NotificationService.Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация EF Core для таблицы outbox_messages
/// </summary>
public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.AggregateType)
            .HasColumnName("aggregate_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.AggregateId)
            .HasColumnName("aggregate_id")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.ProcessedAt)
            .HasColumnName("processed_at");

        builder.Property(x => x.ProcessingAttempts)
            .HasColumnName("processing_attempts")
            .HasDefaultValue(0);

        builder.Property(x => x.ErrorMessage)
            .HasColumnName("error_message");

        builder.Property(x => x.TraceParent)
            .HasColumnName("trace_parent")
            .HasMaxLength(255);

        builder.Property(x => x.TraceState)
            .HasColumnName("trace_state")
            .HasMaxLength(255);

        builder.HasIndex(x => x.TraceParent)
            .HasDatabaseName("ix_outbox_messages_trace_parent");

        builder.HasIndex(x => x.ProcessedAt)
            .HasDatabaseName("ix_outbox_messages_processed_at");
    }
}