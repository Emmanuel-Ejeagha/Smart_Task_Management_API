using System;

namespace SmartTaskManagementAPI.Application.Interfaces;

public interface ITaskCompletionNotificationJob
{
    Task NotifyCompletionAsync(Guid taskId, Guid userId, CancellationToken cancellationToken);
}
