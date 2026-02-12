using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SmartTaskManagement.Domain.Entities;
using SmartTaskManagement.Domain.Entities.Base;
using SmartTaskManagement.Infrastructure.Data.Configurations;
using SmartTaskManagement.Infrastructure.Data.Entities;
using System.Data;

namespace SmartTaskManagement.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets
    public DbSet<WorkItem> WorkItems => Set<WorkItem>();
    public DbSet<Reminder> Reminders => Set<Reminder>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations
        modelBuilder.ApplyConfiguration(new WorkItemConfiguration());
        modelBuilder.ApplyConfiguration(new ReminderConfiguration());
        modelBuilder.ApplyConfiguration(new TenantConfiguration());
        modelBuilder.ApplyConfiguration(new AuditLogConfiguration());

        // Apply soft delete query filters
        // modelBuilder.ApplySoftDeleteQueryFilter<WorkItem>();
        // modelBuilder.ApplySoftDeleteQueryFilter<Reminder>();
        // modelBuilder.ApplySoftDeleteQueryFilter<Tenant>();

        // Configure value objects (if we had them as owned entities)
        // modelBuilder.Owned<Email>();
        // modelBuilder.Owned<PhoneNumber>();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditableEntities();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        UpdateAuditableEntities();
        return base.SaveChanges();
    }

    /// <summary>
    /// Update audit fields automatically
    /// </summary>
    private void UpdateAuditableEntities()
    {
        var entries = ChangeTracker.Entries<AuditableEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = DateTime.UtcNow;
                // CreatedBy is set by the domain entity constructor
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = DateTime.UtcNow;
                // UpdatedBy is set by the domain entity MarkAsUpdated method
                
                // Increment row version for optimistic concurrency
                entry.Entity.IncrementRowVersion();
            }
            else if (entry.State == EntityState.Deleted)
            {
                // Soft delete implementation
                entry.State = EntityState.Modified;
                entry.Entity.DeletedAtUtc = DateTime.UtcNow;
                // DeletedBy is set by the domain entity MarkAsDeleted method
                
                // Increment row version for optimistic concurrency
                entry.Entity.IncrementRowVersion();
            }
        }
    }

    /// <summary>
    /// Begin transaction with explicit isolation level
    /// </summary>
    public async Task<IDbContextTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        return await Database.BeginTransactionAsync(isolationLevel, cancellationToken);
    }

    /// <summary>
    /// Execute raw SQL query
    /// </summary>
    public async Task<IEnumerable<T>> QueryAsync<T>(
        string sql,
        object[] parameters,
        CancellationToken cancellationToken = default)
        where T : class
    {
        return await Set<T>().FromSqlRaw(sql, parameters).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get domain events from all tracked entities
    /// </summary>
    public IEnumerable<object> GetDomainEvents()
    {
        var domainEvents = new List<object>();

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            var entity = entry.Entity;
            domainEvents.AddRange(entity.DomainEvents);
            entity.ClearDomainEvents();
        }

        return domainEvents;
    }

    /// <summary>
    /// Check if database exists and is accessible
    /// </summary>
    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await Database.CanConnectAsync(cancellationToken);
        }
        catch
        {
            return false;
        }
    }
}