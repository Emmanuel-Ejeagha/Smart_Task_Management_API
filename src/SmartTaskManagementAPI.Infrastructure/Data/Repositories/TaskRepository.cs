using Microsoft.EntityFrameworkCore;
using SmartTaskManagementAPI.Application.Common.Models;
using SmartTaskManagementAPI.Application.Interfaces;
using TaskEntity = SmartTaskManagementAPI.Domain.Entities.Task;
using SmartTaskManagementAPI.Domain.Enums;

namespace SmartTaskManagementAPI.Infrastructure.Data.Repositories;

public class TaskRepository : GenericRepository<TaskEntity>, ITaskRepository
{
    public TaskRepository(ApplicationDbContext context) : base(context)
    {
    }

    public override async Task<TaskEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.Tenant)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken);
    }

    public async Task<PaginatedResult<TaskEntity>> GetPaginatedAsync(
        Guid tenantId,
        PaginationQuery pagination,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Where(t => t.TenantId == tenantId && !t.IsDeleted)
            .Include(t => t.Tenant)
            .AsQueryable();

        // Apply search
        if (!string.IsNullOrWhiteSpace(pagination.SearchTerm))
        {
            query = query.Where(t =>
                t.Title.Contains(pagination.SearchTerm) ||
                (t.Description != null && t.Description.Contains(pagination.SearchTerm)));
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = ApplySorting(query, pagination.SortBy, pagination.IsDescending);

        // Apply pagination
        var items = await query
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<TaskEntity>(items, totalCount, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<IEnumerable<TaskEntity>> GetTasksDueForReminderAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        
        return await _dbSet
            .Where(t => t.ReminderDate.HasValue &&
                       t.ReminderDate <= now &&
                       t.Status != TasksStatus.Done &&
                       t.Status != TasksStatus.Archived &&
                       !t.IsDeleted)
            .Include(t => t.Tenant)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TaskEntity>> GetOverdueTasksAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        
        return await _dbSet
            .Where(t => t.DueDate.HasValue &&
                       t.DueDate < now &&
                       t.Status != TasksStatus.Done &&
                       t.Status != TasksStatus.Archived &&
                       !t.IsDeleted)
            .Include(t => t.Tenant)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TaskEntity>> GetTasksByStatusAsync(Guid tenantId, TasksStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.TenantId == tenantId && t.Status == status && !t.IsDeleted)
            .Include(t => t.Tenant)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TaskEntity>> GetTasksByPriorityAsync(Guid tenantId, TaskPriority priority, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.TenantId == tenantId && t.Priority == priority && !t.IsDeleted)
            .Include(t => t.Tenant)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TaskEntity>> GetTasksByUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.TenantId == tenantId && t.CreatedBy == userId && !t.IsDeleted)
            .Include(t => t.Tenant)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .CountAsync(t => t.TenantId == tenantId && !t.IsDeleted, cancellationToken);
    }

    public async Task<int> CountByStatusAsync(Guid tenantId, TasksStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .CountAsync(t => t.TenantId == tenantId && t.Status == status && !t.IsDeleted, cancellationToken);
    }

    public async Task<int> CountOverdueByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        
        return await _dbSet
            .CountAsync(t => t.TenantId == tenantId && 
                            t.DueDate.HasValue &&
                            t.DueDate < now &&
                            t.Status != TasksStatus.Done &&
                            t.Status != TasksStatus.Archived &&
                            !t.IsDeleted, 
                        cancellationToken);
    }

    protected override IQueryable<TaskEntity> ApplySearch(IQueryable<TaskEntity> query, string searchTerm)
    {
        return query.Where(t =>
            t.Title.Contains(searchTerm) ||
            (t.Description != null && t.Description.Contains(searchTerm)));
    }

    protected override IQueryable<TaskEntity> ApplySorting(IQueryable<TaskEntity> query, string? sortBy, bool isDescending)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            return isDescending 
                ? query.OrderByDescending(t => t.CreatedAt) 
                : query.OrderBy(t => t.CreatedAt);
        }

        return sortBy.ToLower() switch
        {
            "title" => isDescending 
                ? query.OrderByDescending(t => t.Title) 
                : query.OrderBy(t => t.Title),
            "priority" => isDescending 
                ? query.OrderByDescending(t => t.Priority) 
                : query.OrderBy(t => t.Priority),
            "status" => isDescending 
                ? query.OrderByDescending(t => t.Status) 
                : query.OrderBy(t => t.Status),
            "duedate" => isDescending 
                ? query.OrderByDescending(t => t.DueDate) 
                : query.OrderBy(t => t.DueDate),
            "createdat" => isDescending 
                ? query.OrderByDescending(t => t.CreatedAt) 
                : query.OrderBy(t => t.CreatedAt),
            "updatedat" => isDescending 
                ? query.OrderByDescending(t => t.UpdatedAt) 
                : query.OrderBy(t => t.UpdatedAt),
            _ => isDescending 
                ? query.OrderByDescending(t => t.CreatedAt) 
                : query.OrderBy(t => t.CreatedAt)
        };
    }
}