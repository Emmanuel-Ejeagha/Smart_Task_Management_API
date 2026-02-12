using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaskManagement.Domain.Entities;
using SmartTaskManagement.Domain.Enums;
using System.Text.Json;

namespace SmartTaskManagement.Infrastructure.Data.Configurations;

public class WorkItemConfiguration : IEntityTypeConfiguration<WorkItem>
{
    public void Configure(EntityTypeBuilder<WorkItem> builder)
    {
        // Table name
        builder.ToTable("work_items");

        // Primary key
        builder.HasKey(w => w.Id);

        // Properties
        builder.Property(w => w.Id)
            .ValueGeneratedNever();

        builder.Property(w => w.TenantId)
            .IsRequired();

        builder.Property(w => w.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(w => w.Description)
            .HasMaxLength(2000);

        builder.Property(w => w.State)
            .IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => (WorkItemState)Enum.Parse(typeof(WorkItemState), v))
            .HasMaxLength(20);

        builder.Property(w => w.Priority)
            .IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => (WorkItemPriority)Enum.Parse(typeof(WorkItemPriority), v))
            .HasMaxLength(20);

        builder.Property(w => w.DueDateUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(w => w.CompletedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(w => w.EstimatedHours)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(w => w.ActualHours)
            .IsRequired()
            .HasDefaultValue(0);

        // FIXED: Tags as JSON array with proper ValueComparer
        builder.Property(w => w.Tags)
            .HasConversion(
                v => JsonSerializer.Serialize(v, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false 
                }),
                v => JsonSerializer.Deserialize<List<string>>(v, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                }) ?? new List<string>(),
                new ValueComparer<List<string>>(
                    (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()))
            .HasMaxLength(2000)
            .HasColumnType("jsonb");

        // Row version for optimistic concurrency
        builder.Property(w => w.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        // Audit properties
        builder.Property(w => w.CreatedAtUtc)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(w => w.CreatedBy)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(w => w.UpdatedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(w => w.UpdatedBy)
            .HasMaxLength(200);

        builder.Property(w => w.DeletedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(w => w.DeletedBy)
            .HasMaxLength(200);

        // Indexes
        builder.HasIndex(w => w.TenantId);
        builder.HasIndex(w => w.State);
        builder.HasIndex(w => w.Priority);
        builder.HasIndex(w => w.DueDateUtc);
        builder.HasIndex(w => w.CreatedAtUtc);
        
        // Composite index for tenant + state for common queries
        builder.HasIndex(w => new { w.TenantId, w.State });
        
        // Composite index for tenant + due date for overdue queries
        builder.HasIndex(w => new { w.TenantId, w.DueDateUtc })
            .HasFilter("\"DueDateUtc\" IS NOT NULL");

        // Relationships
        builder.HasOne(w => w.Tenant)
            .WithMany(t => t.WorkItems)
            .HasForeignKey(w => w.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(w => w.Reminders)
            .WithOne(r => r.WorkItem)
            .HasForeignKey(r => r.WorkItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(e => e.IsDeleted);


        // Apply soft delete query filter
        builder.HasQueryFilter(w => !w.DeletedAtUtc.HasValue);
    }
}