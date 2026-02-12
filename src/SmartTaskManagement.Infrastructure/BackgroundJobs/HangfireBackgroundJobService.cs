using System.Linq.Expressions;
using Hangfire;
using Hangfire.States;
using SmartTaskManagement.Application.Common.Interfaces;

namespace SmartTaskManagement.Infrastructure.BackgroundJobs;

public class HangfireBackgroundJobService : IBackgroundJobService
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IRecurringJobManager _recurringJobManager;

    public HangfireBackgroundJobService(
        IBackgroundJobClient backgroundJobClient,
        IRecurringJobManager recurringJobManager)
    {
        _backgroundJobClient = backgroundJobClient;
        _recurringJobManager = recurringJobManager;
    }

    public string ScheduleReminder(Guid reminderId, DateTime reminderDate)
    {
        // Schedule reminder job to run at the reminder time
        var jobId = _backgroundJobClient.Schedule<ReminderJob>(
            job => job.ProcessReminderAsync(reminderId, JobCancellationToken.Null),
            reminderDate);

        // Also store mapping from reminder ID to job ID for later management
        // In a real app, you might want to store this in a database
        BackgroundJob.ContinueJobWith<ReminderJob>(
            jobId,
            job => job.LogReminderScheduledAsync(reminderId, jobId));

        return jobId;
    }

    public string ScheduleDueRemindersCheck(TimeSpan interval)
    {
        var jobId = $"due-reminders-check-{Guid.NewGuid():N}";

        // Schedule recurring job
        var minutes = (int)interval.TotalMinutes;
        var cronExpression = $"*/{minutes} * * * *";
        _recurringJobManager.AddOrUpdate<DueRemindersCheckJob>(
            jobId,
            job => job.CheckDueRemindersAsync(JobCancellationToken.Null), cronExpression);

        return jobId;
    }

    public bool DeleteJob(string jobId)
    {
        try
        {
            if (BackgroundJob.Delete(jobId))
                return true;

            // Try to remove as recurring job
            _recurringJobManager.RemoveIfExists(jobId);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool RescheduleJob(string jobId, DateTime newDate)
    {
        try
        {
            // For scheduled jobs
            var job = JobStorage.Current.GetConnection().GetJobData(jobId);
            if (job != null)
            {
                BackgroundJob.Delete(jobId);
                
                // Recreate with new schedule
                if (job.Job.Type == typeof(ReminderJob) && 
                    job.Job.Method.Name == nameof(ReminderJob.ProcessReminderAsync))
                {
                    // Extract reminder ID from job args
                    if (job.Job.Args.Count > 0 && job.Job.Args[0] is Guid reminderId)
                    {
                        ScheduleReminder(reminderId, newDate);
                        return true;
                    }
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public bool TriggerJob(string jobId)
    {
        try
        {
            _backgroundJobClient.Requeue(jobId);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Schedule a job with retry policy
    /// </summary>
    public string ScheduleWithRetry<T>(
        Expression<Func<T, Task>> methodCall,
        DateTime scheduleAt,
        int maxRetries = 3,
        TimeSpan? retryDelay = null)
    {
        retryDelay ??= TimeSpan.FromMinutes(5);

        var jobId = _backgroundJobClient.Schedule(methodCall, scheduleAt);

        // Configure retry filter (Hangfire Pro feature or custom filter)
        // For basic implementation, we'll just schedule with default retry

        return jobId;
    }

    /// <summary>
    /// Get job status
    /// </summary>
    public string? GetJobStatus(string jobId)
    {
        try
        {
            var jobData = JobStorage.Current.GetConnection().GetJobData(jobId);
            return jobData?.State;
        }
        catch
        {
            return null;
        }
    }
}