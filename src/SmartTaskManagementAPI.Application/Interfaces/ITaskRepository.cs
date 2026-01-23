using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartTaskManagementAPI.Application.Common.Models;
using TaskEntity = SmartTaskManagementAPI.Domain.Entities.Task;

namespace SmartTaskManagementAPI.Application.Interfaces;

public interface ITaskRepository
{
    Task<TaskEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PaginatedResult<TaskEntity>> GetPaginatedAsync(
        Guid tenantId,
        PaginationQuery pagination,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<TaskEntity>> GetTasksDueForReminderAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<TaskEntity>> GetOverdueTasksAsync(CancellationToken cancellationToken = default);
    Task AddAsync(TaskEntity task, CancellationToken cancellationToken = default);
    void Update(TaskEntity task);
    void Delete(TaskEntity task);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}