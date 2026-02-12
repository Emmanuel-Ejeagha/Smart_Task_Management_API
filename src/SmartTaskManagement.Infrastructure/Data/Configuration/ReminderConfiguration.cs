using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaskManagement.Domain.Entities;
using SmartTaskManagement.Domain.Enums;

namespace SmartTaskManagement.Infrastructure.Data.Configurations;

public class ReminderConfiguration : IEntityTypeConfiguration<Reminder>
{
    public void Configure(EntityTypeBuilder<Reminder> builder)
    {
        // Table name
        builder.ToTable("reminders");

        // Primary key
        builder.HasKey(r => r.Id);

        // Properties
        builder.Property(r => r.Id)
            .ValueGeneratedNever();

        builder.Property(r => r.WorkItemId)
            .IsRequired();

        builder.Property(r => r.ReminderDateUtc)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(r => r.Message)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => (ReminderStatus)Enum.Parse(typeof(ReminderStatus), v))
            .HasMaxLength(20);

        builder.Property(r => r.TriggeredAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(r => r.ErrorMessage)
            .HasMaxLength(1000);

        // Row version for optimistic concurrency
        builder.Property(r => r.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        // Audit properties
        builder.Property(r => r.CreatedAtUtc)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(r => r.CreatedBy)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.UpdatedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(r => r.UpdatedBy)
            .HasMaxLength(200);

        builder.Property(r => r.DeletedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(r => r.DeletedBy)
            .HasMaxLength(200);

        // Indexes
        builder.HasIndex(r => r.WorkItemId);
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.ReminderDateUtc);
        
        // Composite index for performance
        builder.HasIndex(r => new { r.Status, r.ReminderDateUtc })
            .HasFilter("\"Status\" = 'Scheduled'");

        // Relationships
        builder.HasOne(r => r.WorkItem)
            .WithMany(w => w.Reminders)
            .HasForeignKey(r => r.WorkItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(e => e.IsDeleted);

        // Apply soft delete query filter
        builder.HasQueryFilter(r => !r.DeletedAtUtc.HasValue);
    }
}