namespace SmartTaskManagement.Domain.Enums;

/// <summary>
/// Represents the status of a Reminder
/// </summary>
public enum ReminderStatus
{
    /// <summary>
    /// Reminder is scheduled but not yet triggered
    /// </summary>
    Scheduled = 0,
    
    /// <summary>
    /// Reminder has been triggered/sent
    /// </summary>
    Triggered = 1,
    
    /// <summary>
    /// Reminder has been cancelled
    /// </summary>
    Cancelled = 2,
    
    /// <summary>
    /// Reminder failed to trigger
    /// </summary>
    Failed = 3
}