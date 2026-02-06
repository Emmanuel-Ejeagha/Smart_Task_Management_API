using Microsoft.Extensions.Logging;
using SmartTaskManagementAPI.Application.Common.Interfaces;
using SmartTaskManagementAPI.Application.Interfaces;

namespace SmartTaskManagementAPI.Infrastructure.Services;

public class TenantAccessChecker : ITenantAccessChecker
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TenantAccessChecker> _logger;

    public TenantAccessChecker(IUnitOfWork unitOfWork, ILogger<TenantAccessChecker> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> HasAccessToTaskAsync(Guid taskId, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(taskId, cancellationToken);
            if (task == null || task.IsDeleted)
            {
                _logger.LogWarning("Task {TaskId} not found or deleted", taskId);
                return false;
            }

            var user = await _unitOfWork.User.GetByIdAsync(userId, cancellationToken);
            if (user == null || user.IsDeleted)
            {
                _logger.LogWarning("User {UserId} not found or deleted", userId);
                return false;
            }

            return user.TenantId == task.TenantId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking access to task {TaskId} for user {UserId}", taskId, userId);
            return false;
        }
    }

    public async Task<bool> HasAccessToTenantAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.User.GetByIdAsync(userId, cancellationToken);
            if (user == null || user.IsDeleted)
            {
                _logger.LogWarning("User {UserId} not found or deleted", userId);
                return false;
            }

            return user.TenantId == tenantId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking access to tenant {TenantId} for user {UserId}", tenantId, userId);
            return false;
        }
    }
}