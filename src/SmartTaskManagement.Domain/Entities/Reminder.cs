using SmartTaskManagement.Domain.Entities.Base;
using SmartTaskManagement.Domain.Enums;
using SmartTaskManagement.Domain.Events;

namespace SmartTaskManagement.Domain.Entities;

/// <summary>
/// Represents a reminder for a work item
/// </summary>
public class Reminder : AuditableEntity
{
    // Private constructor for EF Core
    private Reminder() { }

    public Reminder(
        Guid workItemId,
        DateTime reminderDateUtc,
        string message,
        string createdBy)
    {
        if (reminderDateUtc <= DateTime.UtcNow)
            throw new ArgumentException("Reminder date must be in the future", nameof(reminderDateUtc));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be null or empty", nameof(message));

        WorkItemId = workItemId;
        ReminderDateUtc = reminderDateUtc;
        Message = message;
        Status = ReminderStatus.Scheduled;

        MarkAsCreated(createdBy);
    }

    public Guid WorkItemId { get; private set; }
    public DateTime ReminderDateUtc { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public ReminderStatus Status { get; private set; }
    public DateTime? TriggeredAtUtc { get; private set; }
    public string? ErrorMessage { get; private set; }

    // Navigation property
    public WorkItem? WorkItem { get; private set; }

    /// <summary>
    /// Update reminder information
    /// </summary>
    public void Update(DateTime reminderDateUtc, string message, string updatedBy)
    {
        if (reminderDateUtc <= DateTime.UtcNow)
            throw new ArgumentException("Reminder date must be in the future", nameof(reminderDateUtc));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be null or empty", nameof(message));

        if (Status != ReminderStatus.Scheduled)
            throw new InvalidOperationException("Can only update scheduled reminders");

        ReminderDateUtc = reminderDateUtc;
        Message = message;

        MarkAsUpdated(updatedBy);
    }

    /// <summary>
    /// Mark the reminder as triggered
    /// </summary>
    public void MarkAsTriggered(string updatedBy)
    {
        if (Status != ReminderStatus.Scheduled)
            throw new InvalidOperationException("Can only trigger scheduled reminders");

        Status = ReminderStatus.Triggered;
        TriggeredAtUtc = DateTime.UtcNow;

        MarkAsUpdated(updatedBy);
        AddDomainEvent(new ReminderTriggeredDomainEvent(this));
    }

    /// <summary>
    /// Mark the reminder as failed
    /// </summary>
    public void MarkAsFailed(string errorMessage, string updatedBy)
    {
        if (Status != ReminderStatus.Scheduled)
            throw new InvalidOperationException("Can only mark scheduled reminders as failed");

        Status = ReminderStatus.Failed;
        ErrorMessage = errorMessage;

        MarkAsUpdated(updatedBy);
    }

    /// <summary>
    /// Cancel the reminder
    /// </summary>
    public void Cancel(string updatedBy)
    {
        if (Status != ReminderStatus.Scheduled)
            throw new InvalidOperationException("Can only cancel scheduled reminders");

        Status = ReminderStatus.Cancelled;

        MarkAsUpdated(updatedBy);
    }

    /// <summary>
    /// Reschedule a cancelled or failed reminder
    /// </summary>
    public void Reschedule(DateTime newReminderDateUtc, string updatedBy)
    {
        if (Status != ReminderStatus.Cancelled && Status != ReminderStatus.Failed)
            throw new InvalidOperationException("Can only reschedule cancelled or failed reminders");

        if (newReminderDateUtc <= DateTime.UtcNow)
            throw new ArgumentException("Reminder date must be in the future", nameof(newReminderDateUtc));

        Status = ReminderStatus.Scheduled;
        ReminderDateUtc = newReminderDateUtc;
        ErrorMessage = null;

        MarkAsUpdated(updatedBy);
    }

    /// <summary>
    /// Check if reminder is pending (scheduled and not yet due)
    /// </summary>
    public bool IsPending()
    {
        return Status == ReminderStatus.Scheduled && ReminderDateUtc > DateTime.UtcNow;
    }

    /// <summary>
    /// Check if reminder is due
    /// </summary>
    public bool IsDue()
    {
        return Status == ReminderStatus.Scheduled && ReminderDateUtc <= DateTime.UtcNow;
    }
}