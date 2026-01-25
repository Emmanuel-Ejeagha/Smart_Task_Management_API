using System;
using SmartTaskManagementAPI.Domain.Entities;
using TaskEntity = SmartTaskManagementAPI.Domain.Entities.Task;

namespace SmartTaskManagementAPI.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    TaskEntity AddAsync(User user, CancellationToken cancellationToken = default);
    void Update(User user);
    Task<User?> GetByIdWithTenantAsync(Guid id, CancellationToken cancellationToken = default);
}
