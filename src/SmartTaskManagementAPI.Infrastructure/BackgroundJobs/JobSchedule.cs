namespace SmartTaskManagementAPI.Infrastructure.BackgroundJobs;

public class JobSchedule
{
    public JobSchedule(Type jobType, string cronExpression, string jobId)
    {
        JobType = jobType;
        CronExpression = cronExpression;
        JobId = jobId;
    }

    public Type JobType { get; }
    public string CronExpression { get; }
    public string JobId { get; }
}

public static class JobSchedules
{
    public static readonly JobSchedule TaskReminderJob = new(
        typeof(TaskReminderJob),
        "0 */1 * * *", // Every hour
        "task-reminders"
    );

    public static readonly JobSchedule EmailNotification = new(
        typeof(EmailNotificationJob),
    "0 */6 * * *", // Every 6 hours
    "email-notifications"
    );

    public static readonly JobSchedule SystemCleanupJob = new(
        typeof(SystemCleanUpJob),
        "0 2 * * *", // Daily at 2am
        "system-cleanup"
    );

    public static readonly JobSchedule DatabaseBackupJob = new(
        typeof(DatabaseBackupJob),
        "0 0 * * 0", // Weekly on Sunday at midnight
        "database-backup"
    );
}
