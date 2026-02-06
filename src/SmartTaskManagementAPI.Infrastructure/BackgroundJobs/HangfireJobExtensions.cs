using Hangfire;
using Hangfire.Dashboard;
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
    
     public static bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        
        // Check if user is authenticated
        if (!httpContext.User.Identity?.IsAuthenticated ?? true)
            return false;
        
        // Check if user has Admin role
        var isAdmin = httpContext.User.IsInRole("Admin");
        
        // For development, also allow if it's localhost
        var isLocal = httpContext.Request.Host.Host == "localhost" || 
                      httpContext.Request.Host.Host == "127.0.0.1";
        
        // Allow access if user is Admin OR if it's local development
        return isAdmin || isLocal;
    }
}