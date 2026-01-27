using System;
using SmartTaskManagementAPI.Application.Common.Models;
using SmartTaskManagementAPI.Domain.Entities;
using TaskEntity = SmartTaskManagementAPI.Domain.Entities.Task;

namespace SmartTaskManagementAPI.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByIdWithTenantAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetUsersByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAndTenantAsync(string email, Guid tenantId, CancellationToken cancellationToken = default);
    Task<PaginatedResult<User>> GetPaginatedByTenantAsync(
        Guid tenantId,
        PaginationQuery pagination,
        CancellationToken cancellationToken = default);
}
