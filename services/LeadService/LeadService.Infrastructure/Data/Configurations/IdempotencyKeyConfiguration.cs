using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LeadService.Domain.Entities;
using SharedKernel.Entities;

namespace LeadService.Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация EF Core для таблицы idempotency_keys
/// </summary>
public class IdempotencyKeyConfiguration : IEntityTypeConfiguration<IdempotencyKey>
{
    public void Configure(EntityTypeBuilder<IdempotencyKey> builder)
    {
        builder.ToTable("idempotency_keys");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.Key)
            .HasColumnName("key")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.LeadId)
            .HasColumnName("lead_id");

        builder.Property(x => x.RequestHash)
            .HasColumnName("request_hash")
            .HasColumnType("bytea")
            .IsRequired();

        builder.Property(x => x.ResponseCode)
            .HasColumnName("response_code");

        builder.Property(x => x.ResponseBody)
            .HasColumnName("response_body")
            .HasColumnType("jsonb");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.LockedUntil)
            .HasColumnName("locked_until");

        builder.HasIndex(x => x.Key)
            .IsUnique()
            .HasDatabaseName("ix_idempotency_keys_key");

        builder.HasOne<Lead>()
            .WithOne()
            .HasForeignKey<IdempotencyKey>(x => x.LeadId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}