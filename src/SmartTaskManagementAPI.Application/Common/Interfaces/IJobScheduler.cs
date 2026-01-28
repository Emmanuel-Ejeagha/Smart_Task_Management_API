namespace SmartTaskManagementAPI.Application.Common.Interfaces;

public interface IJobScheduler
{
    string ScheduleTaskReminder(Guid taskId, Guid userId, DateTime remindAt);
    string ScheduleImmediateTaskReminder(Guid taskId, Guid userId);
    void CancelScheduledReminder(string jobId);
    string EnqueueTaskCompletionNotification(Guid taskId, Guid userId);
}