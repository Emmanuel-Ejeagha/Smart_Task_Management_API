using System;
using Hangfire;

namespace SmartTaskManagementAPI.Infrastructure.BackgroundJobs;

public class RecurringJobsService
{
    private readonly IServiceProvider _serviceProdiver;

    public RecurringJobsService(IServiceProvider serviceProvider)
    {
        _serviceProdiver = serviceProvider;
    }

    public void ScheduleRecurringJobs()
    {
        // Schedule task reminders to run every hour
        RecurringJob.AddOrUpdate<TaskReminderJob>(
            "task-reminders",
            job => job.SendReminderAsync(),
            Cron.Hourly,
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });
    }
}
