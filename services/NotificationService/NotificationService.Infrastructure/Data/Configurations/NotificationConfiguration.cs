using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationService.Domain.Entities;

namespace NotificationService.Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация EF Core для таблицы notifications
/// </summary>
public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.LeadId)
            .HasColumnName("lead_id")
            .IsRequired();

        builder.Property(x => x.EventId)
            .HasColumnName("event_id")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.NotificationType)
            .HasColumnName("notification_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Channel)
            .HasColumnName("channel")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Recipient)
            .HasColumnName("recipient")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Subject)
            .HasColumnName("subject")
            .HasMaxLength(500);

        builder.Property(x => x.Body)
            .HasColumnName("body")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.SentAt)
            .HasColumnName("sent_at");

        builder.Property(x => x.FailureReason)
            .HasColumnName("failure_reason");

        builder.Property(x => x.RetryCount)
            .HasColumnName("retry_count")
            .HasDefaultValue(0);

        builder.Property(x => x.NextRetryAt)
            .HasColumnName("next_retry_at");

        builder.Property(x => x.MaxRetries)
            .HasColumnName("max_retries")
            .HasDefaultValue(3);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(x => x.EventId)
            .IsUnique()
            .HasDatabaseName("ix_notifications_event_id");

        builder.HasIndex(x => x.LeadId)
            .HasDatabaseName("ix_notifications_lead_id");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("ix_notifications_status");

        builder.HasIndex(x => new { x.Status, x.NextRetryAt })
            .HasDatabaseName("ix_notifications_status_next_retry_at");
    }
}