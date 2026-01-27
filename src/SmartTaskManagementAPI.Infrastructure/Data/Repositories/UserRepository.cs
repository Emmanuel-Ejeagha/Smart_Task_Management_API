using Microsoft.EntityFrameworkCore;
using SmartTaskManagementAPI.Application.Common.Models;
using SmartTaskManagementAPI.Application.Interfaces;
using SmartTaskManagementAPI.Domain.Entities;

namespace SmartTaskManagementAPI.Infrastructure.Data.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context)
    {
    }

    public override async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
    }

    public async Task<User?> GetByIdWithTenantAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Email == email.ToLower() && !u.IsDeleted, cancellationToken);
    }

    public async Task<User?> GetByEmailWithTenantAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Email == email.ToLower() && !u.IsDeleted, cancellationToken);
    }

    public async Task<IEnumerable<User>> GetUsersByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(u => u.TenantId == tenantId && !u.IsDeleted)
            .Include(u => u.Tenant)
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(u => u.Email == email.ToLower() && !u.IsDeleted, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAndTenantAsync(string email, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(u => u.Email == email.ToLower() && u.TenantId == tenantId && !u.IsDeleted, cancellationToken);
    }

    public async Task<PaginatedResult<User>> GetPaginatedByTenantAsync(
        Guid tenantId,
        PaginationQuery pagination,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Where(u => u.TenantId == tenantId && !u.IsDeleted)
            .Include(u => u.Tenant)
            .AsQueryable();

        // Apply search
        if (!string.IsNullOrWhiteSpace(pagination.SearchTerm))
        {
            query = query.Where(u =>
                u.FirstName.Contains(pagination.SearchTerm) ||
                u.LastName.Contains(pagination.SearchTerm) ||
                u.Email.Contains(pagination.SearchTerm));
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        if (!string.IsNullOrWhiteSpace(pagination.SortBy))
        {
            query = pagination.SortBy.ToLower() switch
            {
                "firstname" => pagination.IsDescending 
                    ? query.OrderByDescending(u => u.FirstName) 
                    : query.OrderBy(u => u.FirstName),
                "lastname" => pagination.IsDescending 
                    ? query.OrderByDescending(u => u.LastName) 
                    : query.OrderBy(u => u.LastName),
                "email" => pagination.IsDescending 
                    ? query.OrderByDescending(u => u.Email) 
                    : query.OrderBy(u => u.Email),
                "createdat" => pagination.IsDescending 
                    ? query.OrderByDescending(u => u.CreatedAt) 
                    : query.OrderBy(u => u.CreatedAt),
                _ => pagination.IsDescending 
                    ? query.OrderByDescending(u => u.CreatedAt) 
                    : query.OrderBy(u => u.CreatedAt)
            };
        }
        else
        {
            query = pagination.IsDescending 
                ? query.OrderByDescending(u => u.CreatedAt) 
                : query.OrderBy(u => u.CreatedAt);
        }

        // Apply pagination
        var items = await query
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<User>(items, totalCount, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<IEnumerable<User>> GetActiveUsersByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(u => u.TenantId == tenantId && u.IsActive && !u.IsDeleted)
            .Include(u => u.Tenant)
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<User>> GetUsersByRoleAsync(Guid tenantId, string role, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(u => u.TenantId == tenantId && u.Role == role && !u.IsDeleted)
            .Include(u => u.Tenant)
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsEmailUniqueAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(u => u.Email == email.ToLower() && !u.IsDeleted);
        
        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }
        
        return !await query.AnyAsync(cancellationToken);
    }

    public override async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
    }

    public async Task<int> CountByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .CountAsync(u => u.TenantId == tenantId && !u.IsDeleted, cancellationToken);
    }

    public async Task<int> CountActiveByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .CountAsync(u => u.TenantId == tenantId && u.IsActive && !u.IsDeleted, cancellationToken);
    }

    public async Task<User?> GetByDomainUserIdAsync(Guid domainUserId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Id == domainUserId && !u.IsDeleted, cancellationToken);
    }
}