using System;
using Microsoft.EntityFrameworkCore;
using SmartTaskManagementAPI.Application.Interfaces;
using SmartTaskManagementAPI.Domain.Entities;

namespace SmartTaskManagementAPI.Infrastructure.Data.Repositories;

public class TenantRepository : GenericRepository<Tenant>, ITenantRepository
{
    public TenantRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(t => t.Slug == slug.ToLower() && !t.IsDeleted, cancellationToken);
    }

    public async Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(t => t.Slug == slug.ToLower() && !t.IsDeleted, cancellationToken);
    }
}
