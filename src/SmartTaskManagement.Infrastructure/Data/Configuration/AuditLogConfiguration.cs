using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaskManagement.Infrastructure.Data.Entities;
namespace SmartTaskManagement.Infrastructure.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        // Table name
        builder.ToTable("audit_logs");

        // Primary key
        builder.HasKey(a => a.Id);

        // Properties
        builder.Property(a => a.Id)
            .ValueGeneratedOnAdd();

        builder.Property(a => a.TenantId)
            .IsRequired();

        builder.Property(a => a.EntityName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.EntityId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.Action)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.OldValues)
            .HasColumnType("jsonb");

        builder.Property(a => a.NewValues)
            .HasColumnType("jsonb");

        builder.Property(a => a.ChangedBy)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.ChangedAtUtc)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(a => a.IpAddress)
            .HasMaxLength(45); // IPv6 length

        builder.Property(a => a.UserAgent)
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(a => a.TenantId);
        builder.HasIndex(a => a.EntityName);
        builder.HasIndex(a => a.EntityId);
        builder.HasIndex(a => a.Action);
        builder.HasIndex(a => a.ChangedBy);
        builder.HasIndex(a => a.ChangedAtUtc);
        
        // Composite index for common queries
        builder.HasIndex(a => new { a.TenantId, a.EntityName, a.ChangedAtUtc });
    }
}