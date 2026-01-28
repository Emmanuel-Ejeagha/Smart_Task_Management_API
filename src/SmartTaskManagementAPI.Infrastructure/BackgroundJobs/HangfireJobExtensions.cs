using Hangfire;
using SmartTaskManagementAPI.Application.Features.Tasks.Commands.SendTaskReminder;
using SmartTaskManagementAPI.Application.Interfaces;

namespace SmartTaskManagementAPI.Infrastructure.BackgroundJobs;

public static class HangfireJobExtensions
{
    public static string ScheduleTaskReminder(this IBackgroundJobClient backgroundJob, Guid taskId, Guid userId, DateTime remindAt)
    {
        var jobId = backgroundJob.Schedule<SendTaskReminderCommandHandler>(
            handler => handler.Handle(new SendTaskReminderCommand { TaskId = taskId, UserId = userId }, CancellationToken.None),
            remindAt);
        
        return jobId;
    }
    
    public static string ScheduleImmediateTaskReminder(this IBackgroundJobClient backgroundJob, Guid taskId, Guid userId)
    {
        var jobId = backgroundJob.Enqueue<SendTaskReminderCommandHandler>(
            handler => handler.Handle(new SendTaskReminderCommand { TaskId = taskId, UserId = userId }, CancellationToken.None));
        
        return jobId;
    }
    
    public static string ScheduleTaskStatusChangeNotification(
        this IBackgroundJobClient backgroundJob,
        Guid taskId,
        string oldStatus,
        string newStatus,
        Guid changedByUserId)
    {
        // This is an example of another job type you might want to schedule
        var jobId = backgroundJob.Enqueue<ITaskStatusChangeNotificationJob>(
            job => job.NotifyStatusChangeAsync(taskId, oldStatus, newStatus, changedByUserId, CancellationToken.None));
        
        return jobId;
    }
    
    public static void CancelScheduledJob(this IBackgroundJobClient backgroundJob, string jobId)
    {
        BackgroundJob.Delete(jobId);
    }
}