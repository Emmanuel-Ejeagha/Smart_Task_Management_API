using Microsoft.EntityFrameworkCore;
using SmartTaskManagement.Application.Common.Interfaces;
using SmartTaskManagement.Domain.Entities;
using SmartTaskManagement.Infrastructure.Data;

namespace SmartTaskManagement.Infrastructure.Repositories;

public class TenantRepository : GenericRepository<Tenant>, ITenantRepository
{
    public TenantRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Tenant?> GetByAuth0TenantIdAsync(
        string auth0TenantId,
        CancellationToken cancellationToken = default)
    {
        // In a real implementation, you might have a separate mapping table
        // or store Auth0 tenant ID as a property on the Tenant entity
        // For now, we'll assume the tenant ID from Auth0 maps to our Tenant.Id
        
        if (Guid.TryParse(auth0TenantId, out var tenantId))
        {
            return await _context.Tenants
                .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        }

        return null;
    }

    public async Task<bool> IsNameUniqueAsync(
        string name,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Tenants
            .Where(t => t.Name.ToLower() == name.ToLower());

        if (excludeId.HasValue)
        {
            query = query.Where(t => t.Id != excludeId.Value);
        }

        return !await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// Get active tenants
    /// </summary>
    public async Task<IReadOnlyList<Tenant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get tenant with work items count
    /// </summary>
    public async Task<Tenant?> GetByIdWithWorkItemsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .Include(t => t.WorkItems)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }
}