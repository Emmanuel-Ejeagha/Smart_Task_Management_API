namespace SmartTaskManagement.Domain.Events;

/// <summary>
/// Domain event raised when a reminder is scheduled
/// </summary>
public sealed class ReminderScheduledDomainEvent
{
    public ReminderScheduledDomainEvent(Entities.Reminder reminder)
    {
        ReminderId = reminder.Id;
        WorkItemId = reminder.WorkItemId;
        ReminderDateUtc = reminder.ReminderDateUtc;
        Message = reminder.Message;
        ScheduledBy = reminder.CreatedBy;
    }

    public Guid ReminderId { get; }
    public Guid WorkItemId { get; }
    public DateTime ReminderDateUtc { get; }
    public string Message { get; }
    public string ScheduledBy { get; }
}