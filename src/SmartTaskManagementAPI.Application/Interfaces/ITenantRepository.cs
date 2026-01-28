using TenantEntity = SmartTaskManagementAPI.Domain.Entities.Tenant;

namespace SmartTaskManagementAPI.Application.Interfaces;

public interface ITenantRepository
{
    Task<TenantEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TenantEntity?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task AddAsync(TenantEntity tenant, CancellationToken cancellationToken = default);
    Task<IEnumerable<TenantEntity>> GetActiveTenantsAsync(CancellationToken cancellationToken = default);
    Task<int> CountActiveAsync(CancellationToken cancellationToken = default);
}