using TenantEntity = SmartTaskManagementAPI.Domain.Entities.Tenant;

namespace SmartTaskManagementAPI.Application.Interfaces;

public interface ITenantRepository
{
    Task<TenantEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TenantEntity?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task AddAsync(TenantEntity tenant, CancellationToken cancellationToken = default);
}