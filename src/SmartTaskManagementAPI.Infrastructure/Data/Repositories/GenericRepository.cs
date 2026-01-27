using System;
using Microsoft.EntityFrameworkCore;
using SmartTaskManagementAPI.Application.Common.Models;
using SmartTaskManagementAPI.Application.Interfaces;
using SmartTaskManagementAPI.Domain.Entities.Base;

namespace SmartTaskManagementAPI.Infrastructure.Data.Repositories;

public abstract class GenericRepository<T> : IRepository<T> where T : AuditableEntity
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;

    protected GenericRepository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(e => !e.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public virtual async Task<PaginatedResult<T>> GetPaginatedAsync(
        PaginationQuery pagination, 
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(e => !e.IsDeleted);

        // Apply search if SearchTerm is provided
        if (!string.IsNullOrWhiteSpace(pagination.SearchTerm))
        {
            query = ApplySearch(query, pagination.SearchTerm);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = ApplySorting(query, pagination.SortBy, pagination.IsDescending);

        // Apply pagination
        var items = await query
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<T>(items, totalCount, pagination.PageNumber, pagination.PageSize);
    }

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    public virtual void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    public virtual void Delete(T entity)
    {
        // Soft delete by marking as deleted
        entity.MarkAsDeleted(Guid.Empty); // System user or current user will set proper ID
        _dbSet.Update(entity);
    }

    public virtual async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);
    }

    public virtual async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(e => !e.IsDeleted, cancellationToken);
    }

    protected virtual IQueryable<T> ApplySearch(IQueryable<T> query, string searchTerm)
    {
        // Base implementation does no search - override in derived classes
        return query;
    }

    protected virtual IQueryable<T> ApplySorting(IQueryable<T> query, string? sortBy, bool isDescending)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            return isDescending 
                ? query.OrderByDescending(e => e.CreatedAt) 
                : query.OrderBy(e => e.CreatedAt);
        }

        // Try to sort by the specified property
        try
        {
            var propertyInfo = typeof(T).GetProperty(sortBy);
            if (propertyInfo != null)
            {
                return isDescending
                    ? query.OrderByDescending(e => EF.Property<object>(e, sortBy))
                    : query.OrderBy(e => EF.Property<object>(e, sortBy));
            }
        }
        catch
        {
            // Fall back to CreatedAt if property doesn't exist
        }

        return isDescending 
            ? query.OrderByDescending(e => e.CreatedAt) 
            : query.OrderBy(e => e.CreatedAt);
    }
}