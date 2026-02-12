namespace SmartTaskManagement.Domain.Events;

/// <summary>
/// Domain event raised when a reminder is triggered
/// </summary>
public sealed class ReminderTriggeredDomainEvent
{
    public ReminderTriggeredDomainEvent(Entities.Reminder reminder)
    {
        ReminderId = reminder.Id;
        WorkItemId = reminder.WorkItemId;
        TriggeredAtUtc = reminder.TriggeredAtUtc ?? DateTime.UtcNow;
        Message = reminder.Message;
    }

    public Guid ReminderId { get; }
    public Guid WorkItemId { get; }
    public DateTime TriggeredAtUtc { get; }
    public string Message { get; }
}