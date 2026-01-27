using SmartTaskManagementAPI.Application.Common.Models;
using TaskEntity = SmartTaskManagementAPI.Domain.Entities.Task;
using SmartTaskManagementAPI.Domain.Enums;

namespace SmartTaskManagementAPI.Application.Interfaces;

public interface ITaskRepository : IRepository<TaskEntity>
{
    Task<PaginatedResult<TaskEntity>> GetPaginatedAsync(
        Guid tenantId,
        PaginationQuery pagination,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<TaskEntity>> GetTasksDueForReminderAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<TaskEntity>> GetOverdueTasksAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<TaskEntity>> GetTasksByStatusAsync(Guid tenantId, TasksStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<TaskEntity>> GetTasksByPriorityAsync(Guid tenantId, TaskPriority priority, CancellationToken cancellationToken = default);
    Task<IEnumerable<TaskEntity>> GetTasksByUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);
    Task<int> CountByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<int> CountByStatusAsync(Guid tenantId, TasksStatus status, CancellationToken cancellationToken = default);
    Task<int> CountOverdueByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}