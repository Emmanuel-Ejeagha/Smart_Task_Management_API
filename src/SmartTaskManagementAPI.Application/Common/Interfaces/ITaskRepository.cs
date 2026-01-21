using System;
using SmartTaskManagementAPI.Application.Common.Models;
using TaskEntity = SmartTaskManagementAPI.Domain.Entities.Task;

namespace SmartTaskManagementAPI.Application.Common.Interfaces;

public interface ITaskRepository
{
    Task<TaskEntity> GetByIIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PaginatedResult<TaskEntity>> GetPaginatedAsync(
        Guid tenantId,
        PaginationQuery pagination,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<TaskEntity>> GetTaskDueForReminderAsync(CancellationToken cancellationToken = default);
    Task AddAsync(TaskEntity task, CancellationToken cancellationToken = default);
    void Update(TaskEntity task);
    void Delete(TaskEntity task);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
