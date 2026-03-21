using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnrichmentService.Infrastructure.Inbox;

namespace EnrichmentService.Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация EF Core для таблицы inbox_messages
/// </summary>
public class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox_messages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.MessageId)
            .HasColumnName("message_id")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Topic)
            .HasColumnName("topic")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Key)
            .HasColumnName("key")
            .HasMaxLength(255);

        builder.Property(x => x.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.TraceId)
            .HasColumnName("trace_id")
            .HasMaxLength(255);

        builder.Property(x => x.ReceivedAt)
            .HasColumnName("received_at")
            .IsRequired();

        builder.Property(x => x.ProcessedAt)
            .HasColumnName("processed_at");

        builder.Property(x => x.ProcessingAttempts)
            .HasColumnName("processing_attempts")
            .HasDefaultValue(0);

        builder.Property(x => x.ErrorMessage)
            .HasColumnName("error_message");

        builder.Property(x => x.NextRetryAt)
            .HasColumnName("next_retry_at");

        builder.HasIndex(x => x.MessageId)
            .IsUnique()
            .HasDatabaseName("ix_inbox_messages_message_id");

        builder.HasIndex(x => new { x.ProcessedAt, x.NextRetryAt })
            .HasDatabaseName("ix_inbox_messages_processed_at_next_retry_at");
    }
}