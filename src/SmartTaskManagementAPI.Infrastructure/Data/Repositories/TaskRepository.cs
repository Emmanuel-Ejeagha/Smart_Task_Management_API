using System;
using Microsoft.EntityFrameworkCore;
using SmartTaskManagementAPI.Application.Common.Models;
using SmartTaskManagementAPI.Application.Interfaces;
using SmartTaskManagementAPI.Domain.Enums;

namespace SmartTaskManagementAPI.Infrastructure.Data.Repositories;

public class TaskRepository : GenericRepository<Domain.Entities.Task>, ITaskRepository
{
    public TaskRepository(ApplicationDbContext context) : base(context)
    {
    }

    public override async Task<Domain.Entities.Task?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.Tenant)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<PaginatedResult<Domain.Entities.Task>> GetPaginatedAsync(Guid tenantId, PaginationQuery pagination, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Where(t => t.TenantId == tenantId)
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
        if (!string.IsNullOrWhiteSpace(pagination.SortBy))
        {
            query = pagination.SortBy.ToLower() switch
            {
                "title" => pagination.IsDescending
                    ? query.OrderByDescending(t => t.Title)
                    : query.OrderBy(t => t.Title),
                "priority" => pagination.IsDescending
                    ? query.OrderByDescending(t => t.Priority)
                    : query.OrderBy(t => t.Priority),
                "status" => pagination.IsDescending
                    ? query.OrderByDescending(t => t.Status)
                    : query.OrderBy(t => t.Status),
                "duedate" => pagination.IsDescending
                    ? query.OrderByDescending(t => t.DueDate)
                    : query.OrderBy(t => t.DueDate),
                "createdat" => pagination.IsDescending
                    ? query.OrderByDescending(t => t.CreatedAt)
                    : query.OrderBy(t => t.CreatedAt),
                _ => pagination.IsDescending
                    ? query.OrderByDescending(t => t.CreatedAt)
                    : query.OrderBy(t => t.CreatedAt)
            };
        }
        else
        {
            query = pagination.IsDescending
                ? query.OrderByDescending(t => t.CreatedAt)
                : query.OrderBy(t => t.CreatedAt);
        }

        // Apply pagination
        var items = await query
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<Domain.Entities.Task>(items, totalCount, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(t => t.Id == id, cancellationToken);
    }


    public async Task<IEnumerable<Domain.Entities.Task>> GetTasksDueForReminderAsync(CancellationToken cancellationToken = default)
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

    public async Task<IEnumerable<Domain.Entities.Task>> GetOverdueTasksAsync(CancellationToken cancellationToken = default)
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
}
