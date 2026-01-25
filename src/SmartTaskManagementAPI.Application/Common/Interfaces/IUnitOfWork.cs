using System;
using SmartTaskManagementAPI.Application.Interfaces;

namespace SmartTaskManagementAPI.Application.Common.Interfaces;

public interface IUnitOfWork : IDisposable
{
    ITaskRepository Tasks { get; }
    ITenantRepository Tenants { get; }
    IUserRepository User { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
