using Hangfire;
using Microsoft.Extensions.Logging;
using SmartTaskManagementAPI.Application.Common.Interfaces;
using SmartTaskManagementAPI.Application.Features.Tasks.Commands.SendTaskReminder;
using SmartTaskManagementAPI.Application.Interfaces;

namespace SmartTaskManagementAPI.Infrastructure.BackgroundJobs;

public class JobScheduler : IJobScheduler
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<JobScheduler> _logger;

    public JobScheduler(
        IBackgroundJobClient backgroundJobClient,
        ILogger<JobScheduler> logger)
    {
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    public string ScheduleTaskReminder(Guid taskId, Guid userId, DateTime remindAt)
    {
        try
        {
            var jobId = _backgroundJobClient.Schedule<SendTaskReminderCommandHandler>(
                handler => handler.Handle(new SendTaskReminderCommand { TaskId = taskId, UserId = userId }, CancellationToken.None),
                remindAt);
            
            _logger.LogInformation("Scheduled reminder job {JobId} for task {TaskId} at {RemindAt}", 
                jobId, taskId, remindAt);
            
            return jobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule reminder for task {TaskId}", taskId);
            throw;
        }
    }

    public string ScheduleImmediateTaskReminder(Guid taskId, Guid userId)
    {
        try
        {
            var jobId = _backgroundJobClient.Enqueue<SendTaskReminderCommandHandler>(
                handler => handler.Handle(new SendTaskReminderCommand { TaskId = taskId, UserId = userId }, CancellationToken.None));
            
            _logger.LogInformation("Enqueued immediate reminder job {JobId} for task {TaskId}", 
                jobId, taskId);
            
            return jobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue immediate reminder for task {TaskId}", taskId);
            throw;
        }
    }

    public void CancelScheduledReminder(string jobId)
    {
        try
        {
            BackgroundJob.Delete(jobId);
            _logger.LogInformation("Cancelled scheduled job {JobId}", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel scheduled job {JobId}", jobId);
            throw;
        }
    }

    public string EnqueueTaskCompletionNotification(Guid taskId, Guid userId)
    {
        // This is a placeholder for another job type
        var jobId = _backgroundJobClient.Enqueue<ITaskCompletionNotificationJob>(
            job => job.NotifyCompletionAsync(taskId, userId, CancellationToken.None));
        
        return jobId;
    }
}