using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using TaskEntity = SmartTaskManagementAPI.Domain.Entities.Task;
using SmartTaskManagementAPI.Domain.Entities.Base;
using SmartTaskManagementAPI.Infrastructure.Identity;
using System.Reflection;
using SmartTaskManagementAPI.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using System.Linq.Expressions;

namespace SmartTaskManagementAPI.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<TaskEntity> Tasks => Set<TaskEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // IMPORTANT: Call base FIRST to let Identity configure its tables
        base.OnModelCreating(modelBuilder);

        // Rename Identity tables to follow our naming conventions WITH PROPER KEYS
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("IdentityUsers");
            
            // Configure primary key (already done by base, but we can be explicit)
            entity.HasKey(u => u.Id);
            
            // Configure indexes
            entity.HasIndex(u => u.NormalizedEmail).IsUnique();
            entity.HasIndex(u => u.NormalizedUserName).IsUnique();
            entity.HasIndex(u => u.TenantId);
            entity.HasIndex(u => u.DomainUserId);
            entity.HasIndex(u => u.IsActive);
            
            // Configure the relationship between ApplicationUser and Domain User
            entity.HasOne<Domain.Entities.User>()
                .WithMany()
                .HasForeignKey(au => au.DomainUserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ApplicationRole>(entity =>
        {
            entity.ToTable("IdentityRoles");
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => r.NormalizedName).IsUnique();
        });

        modelBuilder.Entity<IdentityUserClaim<Guid>>(entity =>
        {
            entity.ToTable("IdentityUserClaims");
            entity.HasKey(uc => uc.Id);
            entity.HasIndex(uc => uc.UserId);
        });

        modelBuilder.Entity<IdentityUserRole<Guid>>(entity =>
        {
            entity.ToTable("IdentityUserRoles");
            entity.HasKey(ur => new { ur.UserId, ur.RoleId });
            
            // Configure foreign keys
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne<ApplicationRole>()
                .WithMany()
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IdentityUserLogin<Guid>>(entity =>
        {
            entity.ToTable("IdentityUserLogins");
            
            // COMPOSITE KEY: LoginProvider + ProviderKey (as defined by Identity)
            entity.HasKey(l => new { l.LoginProvider, l.ProviderKey });
            
            // Limit the key length since these are used in composite key
            entity.Property(l => l.LoginProvider).HasMaxLength(128);
            entity.Property(l => l.ProviderKey).HasMaxLength(128);
            
            entity.HasIndex(l => l.UserId);
            
            // Configure foreign key
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IdentityRoleClaim<Guid>>(entity =>
        {
            entity.ToTable("IdentityRoleClaims");
            entity.HasKey(rc => rc.Id);
            entity.HasIndex(rc => rc.RoleId);
            
            // Configure foreign key
            entity.HasOne<ApplicationRole>()
                .WithMany()
                .HasForeignKey(rc => rc.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IdentityUserToken<Guid>>(entity =>
        {
            entity.ToTable("IdentityUserTokens");
            
            // COMPOSITE KEY: UserId + LoginProvider + Name (as defined by Identity)
            entity.HasKey(t => new { t.UserId, t.LoginProvider, t.Name });
            
            // Limit the key length since these are used in composite key
            entity.Property(t => t.LoginProvider).HasMaxLength(128);
            entity.Property(t => t.Name).HasMaxLength(128);
        });
        
        // Apply all configurations from this assembly (for our domain entities)
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        
        // Apply global query filters for soft delete (for our domain entities only)
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType) && 
                !typeof(ApplicationUser).IsAssignableFrom(entityType.ClrType) &&
                !typeof(ApplicationRole).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(GetSoftDeleteFilter(entityType.ClrType));
            }
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Set audit fields before saving
        UpdateAuditFields();
        
        var result = await base.SaveChangesAsync(cancellationToken);
        
        return result;
    }

    public override int SaveChanges()
    {
        UpdateAuditFields();
        var result = base.SaveChanges();
        return result;
    }

    private void UpdateAuditFields()
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is AuditableEntity && 
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = (AuditableEntity)entry.Entity;
            
            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
                // Note: CreatedBy will be set by the application layer
            }
            else if (entry.State == EntityState.Modified)
            {
                entity.UpdatedAt = DateTime.UtcNow;
                // Note: UpdatedBy will be set by the application layer
            }
        }
    }

    private static LambdaExpression GetSoftDeleteFilter(Type entityType)
    {
        var parameter = Expression.Parameter(entityType, "e");
        var property = Expression.Property(parameter, nameof(AuditableEntity.IsDeleted));
        var constant = Expression.Constant(false);
        var body = Expression.Equal(property, constant);
        
        return Expression.Lambda(body, parameter);
    }
}