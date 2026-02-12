namespace SmartTaskManagement.Application.Common.Interfaces;

/// <summary>
/// Background job service interface for scheduling and managing background jobs
/// </summary>
public interface IBackgroundJobService
{
    /// <summary>
    /// Schedule a reminder job
    /// </summary>
    string ScheduleReminder(Guid reminderId, DateTime reminderDate);

    /// <summary>
    /// Schedule a recurring job for due reminders check
    /// </summary>
    string ScheduleDueRemindersCheck(TimeSpan interval);

    /// <summary>
    /// Delete a scheduled job
    /// </summary>
    bool DeleteJob(string jobId);

    /// <summary>
    /// Reschedule a job
    /// </summary>
    bool RescheduleJob(string jobId, DateTime newDate);

    /// <summary>
    /// Trigger a job immediately
    /// </summary>
    bool TriggerJob(string jobId);
}