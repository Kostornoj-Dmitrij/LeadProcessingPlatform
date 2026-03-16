using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LeadService.Infrastructure.Inbox;

namespace LeadService.Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация EF Core для таблицы InboxMessages
/// </summary>
public class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("InboxMessages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MessageId)
            .HasColumnName("MessageId")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Topic)
            .HasColumnName("Topic")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Key)
            .HasColumnName("Key")
            .HasMaxLength(255);

        builder.Property(x => x.EventType)
            .HasColumnName("EventType")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Payload)
            .HasColumnName("Payload")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.ReceivedAt)
            .HasColumnName("ReceivedAt")
            .IsRequired();

        builder.Property(x => x.ProcessedAt)
            .HasColumnName("ProcessedAt");

        builder.Property(x => x.ProcessingAttempts)
            .HasColumnName("ProcessingAttempts")
            .HasDefaultValue(0);

        builder.Property(x => x.ErrorMessage)
            .HasColumnName("ErrorMessage");

        builder.Property(x => x.NextRetryAt)
            .HasColumnName("NextRetryAt");

        builder.HasIndex(x => x.MessageId)
            .IsUnique()
            .HasDatabaseName("IX_InboxMessages_MessageId");

        builder.HasIndex(x => new { x.ProcessedAt, x.NextRetryAt })
            .HasDatabaseName("IX_InboxMessages_ProcessedAt_NextRetryAt");
    }
}