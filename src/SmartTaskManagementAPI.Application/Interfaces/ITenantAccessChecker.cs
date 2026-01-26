using TaskEntity = SmartTaskManagementAPI.Domain.Entities.Task;

namespace SmartTaskManagementAPI.Application.Interfaces;

public interface ITenantAccessChecker
{
    Task CheckTaskAccessAsync(
        TaskEntity task,
        Guid userId,
        CancellationToken cancellationToken);
}
