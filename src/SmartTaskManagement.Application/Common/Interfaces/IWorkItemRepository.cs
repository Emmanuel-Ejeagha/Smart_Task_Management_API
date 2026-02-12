using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Domain.Entities;
using SmartTaskManagement.Domain.Enums;

namespace SmartTaskManagement.Application.Common.Interfaces;

/// <summary>
/// WorkItem-specific repository interface with specialized queries
/// </summary>
public interface IWorkItemRepository : IRepository<WorkItem>
{
    /// <summary>
    /// Get work items by tenant ID with pagination and filtering
    /// </summary>
    Task<PaginatedResult<WorkItem>> GetByTenantIdAsync(
        Guid tenantId,
        PaginationRequest pagination,
        SortingRequest? sorting = null,
        FilteringRequest? filtering = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get work items by state
    /// </summary>
    Task<IReadOnlyList<WorkItem>> GetByStateAsync(
        Guid tenantId,
        WorkItemState state,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get overdue work items for a tenant
    /// </summary>
    Task<IReadOnlyList<WorkItem>> GetOverdueAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get work items with reminders due soon
    /// </summary>
    Task<IReadOnlyList<WorkItem>> GetWithRemindersDueSoonAsync(
        Guid tenantId,
        DateTime cutoffDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if work item title is unique within tenant
    /// </summary>
    Task<bool> IsTitleUniqueAsync(
        Guid tenantId,
        string title,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);
}