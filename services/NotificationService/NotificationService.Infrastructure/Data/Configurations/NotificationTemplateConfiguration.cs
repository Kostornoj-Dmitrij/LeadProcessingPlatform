using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationService.Domain.Entities;

namespace NotificationService.Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация EF Core для таблицы notification_templates
/// </summary>
public class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("notification_templates");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.TemplateType)
            .HasColumnName("template_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Channel)
            .HasColumnName("channel")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.SubjectTemplate)
            .HasColumnName("subject_template")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.BodyTemplate)
            .HasColumnName("body_template")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.Variables)
            .HasColumnName("variables")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasIndex(x => new { x.TemplateType, x.Channel })
            .IsUnique()
            .HasDatabaseName("ix_notification_templates_template_type_channel");
    }
}