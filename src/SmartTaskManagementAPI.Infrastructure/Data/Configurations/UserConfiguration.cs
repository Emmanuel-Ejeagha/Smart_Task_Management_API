using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaskManagementAPI.Domain.Entities;

namespace SmartTaskManagementAPI.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.NormalizedEmail)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.PasswordHash)
            .IsRequired();

        builder.Property(u => u.PhoneNumber)
            .HasMaxLength(20);

        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(u => u.EmailConfirmed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.Role)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("User");

        // Indexes
        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.HasIndex(u => u.NormalizedEmail)
            .IsUnique();

        builder.HasIndex(u => u.TenantId);

        builder.HasIndex(u => u.IsActive);

        builder.HasIndex(u => u.Role);

        // Foreign key
        builder.HasOne(u => u.Tenant)
            .WithMany(t => t.Users)
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relationship with tasks
        builder.HasMany(u => u.CreatedTasks)
            .WithOne()
            .HasForeignKey(t => t.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.UpdatedTasks)
            .WithOne()
            .HasForeignKey(t => t.UpdatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Audit fields
        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.CreatedBy);

        builder.Property(u => u.UpdatedAt);

        builder.Property(u => u.UpdatedBy);

        builder.Property(u => u.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.DeletedAt);

        builder.Property(u => u.DeletedBy);
    }
}
