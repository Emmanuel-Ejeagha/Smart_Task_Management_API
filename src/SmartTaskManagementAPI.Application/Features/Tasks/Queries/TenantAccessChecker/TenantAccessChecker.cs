using System;
using SmartTaskManagementAPI.Application.Common.Exceptions;
using SmartTaskManagementAPI.Application.Common.Interfaces;
using SmartTaskManagementAPI.Application.Interfaces;
using TaskEntity = SmartTaskManagementAPI.Domain.Entities.Task;

namespace SmartTaskManagementAPI.Application.Features.Tasks.Queries.TenantAccessChecker;

public class TenantAccessChecker : ITenantAccessChecker
{
    private readonly IUnitOfWork _unitOfWork;

    public TenantAccessChecker(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task CheckTaskAccessAsync(
        TaskEntity task,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.User.GetByIdAsync(userId, cancellationToken);
        if (user == null || user.IsDeleted)
            throw new NotFoundException("User", userId);

        if (user.TenantId != task.TenantId)
            throw new UnauthorizedAccessException("Access denied to task from different tenant");
    }
}
