using Microsoft.EntityFrameworkCore;
using SmartTaskManagement.Application.Common.Interfaces;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Domain.Entities;
using SmartTaskManagement.Domain.Enums;
using SmartTaskManagement.Infrastructure.Data;

namespace SmartTaskManagement.Infrastructure.Repositories;

public class WorkItemRepository : GenericRepository<WorkItem>, IWorkItemRepository
{
    public WorkItemRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<PaginatedResult<WorkItem>> GetByTenantIdAsync(
        Guid tenantId,
        PaginationRequest pagination,
        SortingRequest? sorting = null,
        FilteringRequest? filtering = null,
        CancellationToken cancellationToken = default)
    {
        // Start with base query
        var query = _context.WorkItems
            .Include(w => w.Reminders)
            .Where(w => w.TenantId == tenantId);

        // Apply filtering
        if (filtering?.IsSpecified == true)
        {
            query = ApplyFiltering(query, filtering);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        if (sorting?.IsSpecified == true)
        {
            query = ApplySorting(query, sorting);
        }
        else
        {
            // Default sorting by creation date (newest first)
            query = query.OrderByDescending(w => w.CreatedAtUtc);
        }

        // Apply pagination
        var items = await query
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return PaginatedResult<WorkItem>.Create(items, pagination.PageNumber, pagination.PageSize, totalCount);
    }

    public async Task<IReadOnlyList<WorkItem>> GetByStateAsync(
        Guid tenantId,
        WorkItemState state,
        CancellationToken cancellationToken = default)
    {
        return await _context.WorkItems
            .Where(w => w.TenantId == tenantId && w.State == state)
            .OrderByDescending(w => w.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkItem>> GetOverdueAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _context.WorkItems
            .Where(w => w.TenantId == tenantId &&
                       w.DueDateUtc.HasValue &&
                       w.DueDateUtc.Value < now &&
                       w.State != WorkItemState.Completed &&
                       w.State != WorkItemState.Archived &&
                       w.State != WorkItemState.Cancelled)
            .OrderBy(w => w.DueDateUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkItem>> GetWithRemindersDueSoonAsync(
        Guid tenantId,
        DateTime cutoffDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.WorkItems
            .Include(w => w.Reminders)
            .Where(w => w.TenantId == tenantId &&
                       w.Reminders.Any(r => 
                           r.Status == ReminderStatus.Scheduled &&
                           r.ReminderDateUtc <= cutoffDate))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsTitleUniqueAsync(
        Guid tenantId,
        string title,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.WorkItems
            .Where(w => w.TenantId == tenantId &&
                       w.Title.ToLower() == title.ToLower());

        if (excludeId.HasValue)
        {
            query = query.Where(w => w.Id != excludeId.Value);
        }

        return !await query.AnyAsync(cancellationToken);
    }

    private IQueryable<WorkItem> ApplyFiltering(IQueryable<WorkItem> query, FilteringRequest filtering)
    {
        return filtering.Field.ToLower() switch
        {
            "title" => filtering.Operator switch
            {
                FilteringRequest.FilterOperator.Contains => 
                    query.Where(w => w.Title.Contains(filtering.Value)),
                FilteringRequest.FilterOperator.StartsWith => 
                    query.Where(w => w.Title.StartsWith(filtering.Value)),
                FilteringRequest.FilterOperator.Equals => 
                    query.Where(w => w.Title == filtering.Value),
                _ => query
            },
            "state" => Enum.TryParse<WorkItemState>(filtering.Value, out var state)
                ? query.Where(w => w.State == state)
                : query,
            "priority" => Enum.TryParse<WorkItemPriority>(filtering.Value, out var priority)
                ? query.Where(w => w.Priority == priority)
                : query,
            "duedate" => DateTime.TryParse(filtering.Value, out var dueDate)
                ? filtering.Operator switch
                {
                    FilteringRequest.FilterOperator.GreaterThan => 
                        query.Where(w => w.DueDateUtc > dueDate),
                    FilteringRequest.FilterOperator.LessThan => 
                        query.Where(w => w.DueDateUtc < dueDate),
                    FilteringRequest.FilterOperator.Equals => 
                        query.Where(w => w.DueDateUtc == dueDate),
                    _ => query
                }
                : query,
            "tag" => query.Where(w => w.Tags.Any(t => t.Contains(filtering.Value))),
            _ => query
        };
    }

    private IQueryable<WorkItem> ApplySorting(IQueryable<WorkItem> query, SortingRequest sorting)
    {
        var direction = sorting.Direction == SortingRequest.SortDirection.Ascending
            ? "asc"
            : "desc";

        return sorting.SortBy.ToLower() switch
        {
            "title" => direction == "asc"
                ? query.OrderBy(w => w.Title)
                : query.OrderByDescending(w => w.Title),
            "priority" => direction == "asc"
                ? query.OrderBy(w => w.Priority)
                : query.OrderByDescending(w => w.Priority),
            "duedate" => direction == "asc"
                ? query.OrderBy(w => w.DueDateUtc)
                : query.OrderByDescending(w => w.DueDateUtc),
            "createdat" => direction == "asc"
                ? query.OrderBy(w => w.CreatedAtUtc)
                : query.OrderByDescending(w => w.CreatedAtUtc),
            "state" => direction == "asc"
                ? query.OrderBy(w => w.State)
                : query.OrderByDescending(w => w.State),
            _ => query.OrderByDescending(w => w.CreatedAtUtc)
        };
    }
}