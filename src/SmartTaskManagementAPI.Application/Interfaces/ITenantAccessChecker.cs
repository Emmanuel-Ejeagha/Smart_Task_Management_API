namespace SmartTaskManagementAPI.Application.Common.Interfaces;

public interface ITenantAccessChecker
{
    Task<bool> HasAccessToTaskAsync(Guid taskId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> HasAccessToTenantAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);
}
