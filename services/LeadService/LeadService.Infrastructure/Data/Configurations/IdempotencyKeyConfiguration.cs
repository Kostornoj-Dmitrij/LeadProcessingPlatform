using LeadService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedKernel.Entities;

namespace LeadService.Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация EF Core для таблицы IdempotencyKeys
/// </summary>
public class IdempotencyKeyConfiguration : IEntityTypeConfiguration<IdempotencyKey>
{
    public void Configure(EntityTypeBuilder<IdempotencyKey> builder)
    {
        builder.ToTable("IdempotencyKeys");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Key)
            .HasColumnName("IdempotencyKey")
            .HasMaxLength(255)
            .IsRequired();
        
        builder.Property(x => x.LeadId)
            .HasColumnName("LeadId");
        
        builder.Property(x => x.RequestHash)
            .HasColumnName("RequestHash")
            .HasColumnType("bytea")
            .IsRequired();
        
        builder.Property(x => x.ResponseCode)
            .HasColumnName("ResponseCode");
        
        builder.Property(x => x.ResponseBody)
            .HasColumnName("ResponseBody")
            .HasColumnType("jsonb");
        
        builder.Property(x => x.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();
        
        builder.Property(x => x.LockedUntil)
            .HasColumnName("LockedUntil");
        
        builder.HasIndex(x => x.Key)
            .IsUnique()
            .HasDatabaseName("IX_IdempotencyKeys_Key");
        
        builder.HasOne<Lead>()
            .WithOne()
            .HasForeignKey<IdempotencyKey>(x => x.LeadId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}