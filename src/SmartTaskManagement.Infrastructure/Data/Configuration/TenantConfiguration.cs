using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaskManagement.Domain.Entities;

namespace SmartTaskManagement.Infrastructure.Data.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        // Table name
        builder.ToTable("tenants");

        // Primary key
        builder.HasKey(t => t.Id);

        // Properties
        builder.Property(t => t.Id)
            .ValueGeneratedNever();

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.Description)
            .HasMaxLength(500);

        builder.Property(t => t.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Row version for optimistic concurrency
        builder.Property(t => t.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        // Audit properties
        builder.Property(t => t.CreatedAtUtc)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(t => t.CreatedBy)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.UpdatedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(t => t.UpdatedBy)
            .HasMaxLength(200);

        builder.Property(t => t.DeletedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(t => t.DeletedBy)
            .HasMaxLength(200);

        // Indexes
        builder.HasIndex(t => t.Name)
            .IsUnique()
            .HasFilter("\"DeletedAtUtc\" IS NULL");

        builder.HasIndex(t => t.IsActive);
        builder.HasIndex(t => t.CreatedAtUtc);

        // Relationships
        builder.HasMany(t => t.WorkItems)
            .WithOne(w => w.Tenant)
            .HasForeignKey(w => w.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.Ignore(e => e.IsDeleted);


        // Apply soft delete query filter
        builder.HasQueryFilter(t => !t.DeletedAtUtc.HasValue);
    }
}