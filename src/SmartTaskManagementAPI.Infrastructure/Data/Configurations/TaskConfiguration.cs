using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskEntity = SmartTaskManagementAPI.Domain.Entities.Task;
using SmartTaskManagementAPI.Domain.Enums;

namespace SmartTaskManagementAPI.Infrastructure.Data.Configurations;

public class TaskConfiguration : IEntityTypeConfiguration<TaskEntity>
{
    public void Configure(EntityTypeBuilder<TaskEntity> builder)
    {
        builder.ToTable("Tasks");
        
        builder.HasKey(t => t.Id);
        
        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(t => t.Description)
            .HasMaxLength(1000);
        
        // FIXED: Proper enum configuration with sentinel values
        builder.Property(t => t.Priority)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(TaskPriority.Medium)
            .HasSentinel(TaskPriority.Medium);
        
        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(TasksStatus.Draft)
            .HasSentinel(TasksStatus.Draft);
        
        builder.Property(t => t.DueDate);
        
        builder.Property(t => t.ReminderDate);
        
        // Indexes
        builder.HasIndex(t => t.TenantId);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.Priority);
        builder.HasIndex(t => t.DueDate);
        builder.HasIndex(t => t.CreatedBy);
        builder.HasIndex(t => t.CreatedAt);
        builder.HasIndex(t => new { t.TenantId, t.DueDate });
        builder.HasIndex(t => new { t.TenantId, t.Status, t.DueDate });
        
        // Foreign key
        builder.HasOne(t => t.Tenant)
            .WithMany(t => t.Tasks)
            .HasForeignKey(t => t.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Audit fields
        builder.Property(t => t.CreatedAt)
            .IsRequired();
        
        builder.Property(t => t.CreatedBy);
        
        builder.Property(t => t.UpdatedAt);
        
        builder.Property(t => t.UpdatedBy);
        
        builder.Property(t => t.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);
        
        builder.Property(t => t.DeletedAt);
        
        builder.Property(t => t.DeletedBy);
    }
}