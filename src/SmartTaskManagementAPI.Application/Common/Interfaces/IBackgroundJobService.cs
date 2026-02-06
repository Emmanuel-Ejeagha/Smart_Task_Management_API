namespace SmartTaskManagementAPI.Application.Common.Interfaces;

public interface IBackgroundJobService
{
    string ScheduleTaskReminder(Guid taskId, Guid userId, DateTime remindAt);
    string ScheduleImmediateTaskReminder(Guid taskId, Guid userId);
    void CancelScheduledReminder(string jobId);
    string EnqueueTaskCompletionNotification(Guid taskId, Guid userId);
}