using System;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Common;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SmartTaskManagementAPI.Infrastructure.BackgroundJobs;

public class RecurringJobsService : IHostedService
{
    private readonly ILogger<RecurringJobsService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public RecurringJobsService(
        ILogger<RecurringJobsService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting recurring jobs service...");
            
            // Schedule task reminders to run every 30 minutes
            RecurringJob.AddOrUpdate<TaskReminderJob>(
                "task-reminders",
                job => job.SendRemindersAsync(),
                "*/30 * * * *"); // Every 30 minutes
            
            _logger.LogInformation("Scheduled task reminders job (every 30 minutes)");
            
            // Schedule overdue task notifications to run daily at 9 AM UTC
            RecurringJob.AddOrUpdate<OverdueTaskNotificationJob>(
                "overdue-task-notifications",
                job => job.SendOverdueNotificationsAsync(),
                "0 9 * * *"); // Daily at 9:00 AM UTC
            
            _logger.LogInformation("Scheduled overdue task notifications job (daily at 9 AM UTC)");
            
            // Schedule database cleanup job to run weekly on Sunday at 2 AM UTC
            RecurringJob.AddOrUpdate<DatabaseCleanupJob>(
                "database-cleanup",
                job => job.CleanupOldDataAsync(),
                "0 2 * * 0"); // Weekly on Sunday at 2:00 AM UTC
            
            _logger.LogInformation("Scheduled database cleanup job (weekly on Sunday at 2 AM UTC)");
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule recurring jobs");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping recurring jobs service...");
        return Task.CompletedTask;
    }
    public void ScheduleRecurringJobs()
    {
        // Updated call for TaskReminderJob
        RecurringJob.AddOrUpdate<TaskReminderJob>(
            "task-reminders",
            job => job.SendRemindersAsync(),
            Cron.Hourly); // Removed the TimeZoneInfo parameter

        // Updated call for OverdueTaskNotificationJob
        RecurringJob.AddOrUpdate<OverdueTaskNotificationJob>(
            "overdue-task-notifications",
            job => job.SendOverdueNotificationsAsync(),
            Cron.Daily); // Runs daily at midnight UTC. Removed TimeZoneInfo.

        // Updated call for DatabaseCleanupJob
        RecurringJob.AddOrUpdate<DatabaseCleanupJob>(
            "database-cleanup",
            job => job.CleanupOldDataAsync(),
            Cron.Weekly(DayOfWeek.Sunday, 2)); // Runs Sunday at 02:00 UTC.
    }
}