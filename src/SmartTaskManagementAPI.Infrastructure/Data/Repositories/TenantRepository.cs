using Microsoft.EntityFrameworkCore;
using SmartTaskManagementAPI.Application.Features.Tasks.DTOs;
using SmartTaskManagementAPI.Application.Interfaces;
using SmartTaskManagementAPI.Domain.Entities;

namespace SmartTaskManagementAPI.Infrastructure.Data.Repositories;

public class TenantRepository : GenericRepository<Tenant>, ITenantRepository
{
    public TenantRepository(ApplicationDbContext context) : base(context)
    {
    }

    public override async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken);
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

    public async Task<IEnumerable<Tenant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.IsActive && !t.IsDeleted)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .CountAsync(t => t.IsActive && !t.IsDeleted, cancellationToken);
    }

    public async Task<IEnumerable<TenantWithStatsDto>> GetTenantsWithStatsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => !t.IsDeleted)
            .Select(t => new TenantWithStatsDto
            {
                Id = t.Id,
                Name = t.Name,
                Slug = t.Slug,
                IsActive = t.IsActive,
                CreatedAt = t.CreatedAt,
                UserCount = t.Users.Count(u => !u.IsDeleted),
                ActiveUserCount = t.Users.Count(u => !u.IsDeleted && u.IsActive),
                TaskCount = t.Tasks.Count(task => !task.IsDeleted)
            })
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Tenant?> GetTenantWithUsersAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.Users.Where(u => !u.IsDeleted))
            .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsDeleted, cancellationToken);
    }

    public async Task<Tenant?> GetTenantWithTasksAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.Tasks.Where(task => !task.IsDeleted))
            .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsDeleted, cancellationToken);
    }

    public async Task<Tenant?> GetTenantFullDetailsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.Users.Where(u => !u.IsDeleted))
            .Include(t => t.Tasks.Where(task => !task.IsDeleted))
            .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsDeleted, cancellationToken);
    }

    protected override IQueryable<Tenant> ApplySearch(IQueryable<Tenant> query, string searchTerm)
    {
        return query.Where(t => 
            t.Name.Contains(searchTerm) || 
            t.Slug.Contains(searchTerm));
    }

    protected override IQueryable<Tenant> ApplySorting(IQueryable<Tenant> query, string? sortBy, bool isDescending)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            return isDescending 
                ? query.OrderByDescending(t => t.CreatedAt) 
                : query.OrderBy(t => t.CreatedAt);
        }

        return sortBy.ToLower() switch
        {
            "name" => isDescending 
                ? query.OrderByDescending(t => t.Name) 
                : query.OrderBy(t => t.Name),
            "slug" => isDescending 
                ? query.OrderByDescending(t => t.Slug) 
                : query.OrderBy(t => t.Slug),
            "isactive" => isDescending 
                ? query.OrderByDescending(t => t.IsActive) 
                : query.OrderBy(t => t.IsActive),
            "createdat" => isDescending 
                ? query.OrderByDescending(t => t.CreatedAt) 
                : query.OrderBy(t => t.CreatedAt),
            _ => isDescending 
                ? query.OrderByDescending(t => t.CreatedAt) 
                : query.OrderBy(t => t.CreatedAt)
        };
    }
}