using System;
using SmartTaskManagementAPI.Application.Common.Interfaces;
using SmartTaskManagementAPI.Application.Interfaces;
using SmartTaskManagementAPI.Infrastructure.Data.Repositories;

namespace SmartTaskManagementAPI.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private ITaskRepository? _task;
    private ITenantRepository? _tenants;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public ITaskRepository Tasks => _task ??= new TaskRepository(_context);

    public ITenantRepository Tenants => _tenants ??= new TenantRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
