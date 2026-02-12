using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using SmartTaskManagement.Domain.Entities.Base;

namespace SmartTaskManagement.Infrastructure.Data.Configurations;

/// <summary>
/// Extension methods for soft delete query filters
/// </summary>
public static class SoftDeleteQueryFilter
{
    /// <summary>
    /// Apply soft delete filter to entity
    /// </summary>
    public static void ApplySoftDeleteQueryFilter<T>(this ModelBuilder modelBuilder) 
        where T : AuditableEntity
    {
        modelBuilder.Entity<T>().HasQueryFilter(e => !e.IsDeleted);
    }

    /// <summary>
    /// Get entities including soft deleted ones
    /// </summary>
    public static IQueryable<T> IncludeDeleted<T>(this IQueryable<T> query) 
        where T : AuditableEntity
    {
        return query.IgnoreQueryFilters();
    }

    /// <summary>
    /// Get only deleted entities
    /// </summary>
    public static IQueryable<T> OnlyDeleted<T>(this IQueryable<T> query) 
        where T : AuditableEntity
    {
        return query.IgnoreQueryFilters().Where(e => e.IsDeleted);
    }
}