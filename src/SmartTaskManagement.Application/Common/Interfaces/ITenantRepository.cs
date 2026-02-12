using SmartTaskManagement.Domain.Entities;

namespace SmartTaskManagement.Application.Common.Interfaces;

/// <summary>
/// Tenant-specific repository interface
/// </summary>
public interface ITenantRepository : IRepository<Tenant>
{
    /// <summary>
    /// Get tenant by Auth0 tenant ID (from claims)
    /// </summary>
    Task<Tenant?> GetByAuth0TenantIdAsync(
        string auth0TenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if tenant name is unique
    /// </summary>
    Task<bool> IsNameUniqueAsync(
        string name,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);
}