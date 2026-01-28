using System;

namespace SmartTaskManagementAPI.Application.Interfaces;

public interface ITaskStatusChangeNotificationJob
{
    Task NotifyStatusChangeAsync(Guid taskId, string oldStatus, string newStatus, Guid changedByUserId, CancellationToken cancellationToken);
}
