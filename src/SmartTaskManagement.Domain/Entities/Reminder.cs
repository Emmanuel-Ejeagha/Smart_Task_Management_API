using SmartTaskManagement.Domain.Common;
using SmartTaskManagement.Domain.Enums;
using SmartTaskManagement.Domain.Events;

namespace SmartTaskManagement.Domain.Entities;

/// <summary>
/// Represents a reminder for a WorkItem.
/// Junior Developer Explanation: Reminders are scheduled notifications
/// that remind users about WorkItems. They can be one-time or recurring.
/// Reminder is a child entity of WorkItem (it belongs to a WorkItem).
/// </summary>
public sealed class Reminder : AuditableEntity
{
    /// <summary>
    /// Type of reminder (Notification, Email, SMS, etc.).
    /// </summary>
    public ReminderType Type { get; private set; }

    /// <summary>
    /// Message to display/send in the reminder.
    /// </summary>
    public string Message { get; private set; }

    /// <summary>
    /// When the reminder is scheduled to trigger.
    /// </summary>
    public DateTime ScheduledTime { get; private set; }

    /// <summary>
    /// When the reminder was actually sent (if sent).
    /// </summary>
    public DateTime? SentAt { get; private set; }

    /// <summary>
    /// ID of the WorkItem this reminder is for.
    /// </summary>
    public Guid WorkItemId { get; private set; }

    /// <summary>
    /// ID of the tenant that owns this reminder.
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Whether the reminder is active (will trigger).
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Recurrence pattern for the reminder (optional).
    /// Example: "Daily", "Weekly", "Monthly".
    /// </summary>
    public string? RecurrencePattern { get; private set; }

    /// <summary>
    /// Private constructor for EF Core.
    /// </summary>
    private Reminder()
    {
        Message = string.Empty;
    }

    /// <summary>
    /// Creates a new Reminder.
    /// </summary>
    public Reminder(
        Guid workItemId,
        Guid tenantId,
        string message,
        DateTime scheduledTime,
        string createdBy,
        ReminderType type = ReminderType.Notification,
        string? recurrencePattern = null)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message is required.", nameof(message));
        
        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("CreatedBy is required.", nameof(createdBy));
        
        if (scheduledTime <= DateTime.UtcNow)
            throw new ArgumentException("Scheduled time must be in the future.", nameof(scheduledTime));

        WorkItemId = workItemId;
        TenantId = tenantId;
        Message = message.Trim();
        ScheduledTime = scheduledTime;
        Type = type;
        RecurrencePattern = recurrencePattern;

        MarkAsCreated(createdBy);
    }

    /// <summary>
    /// Updates the reminder message.
    /// </summary>
    public void UpdateMessage(string message, string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message is required.", nameof(message));

        Message = message.Trim();
        MarkAsUpdated(updatedBy);
    }

    /// <summary>
    /// Reschedules the reminder.
    /// </summary>
    public void Reschedule(DateTime newScheduledTime, string updatedBy)
    {
        if (newScheduledTime <= DateTime.UtcNow)
            throw new ArgumentException("New scheduled time must be in the future.", nameof(newScheduledTime));

        ScheduledTime = newScheduledTime;
        MarkAsUpdated(updatedBy);
    }

    /// <summary>
    /// Marks the reminder as sent.
    /// </summary>
    public void MarkAsSent()
    {
        if (SentAt.HasValue)
            throw new InvalidOperationException("Reminder has already been sent.");

        SentAt = DateTime.UtcNow;
        
        // Raise domain event
        AddDomainEvent(new ReminderTriggeredEvent(
            Id,
            WorkItemId,
            string.Empty, // Will be populated by infrastructure
            CreatedBy,
            Message,
            ScheduledTime));
    }

    /// <summary>
    /// Activates the reminder.
    /// </summary>
    public void Activate(string updatedBy)
    {
        if (IsActive)
            throw new InvalidOperationException("Reminder is already active.");

        IsActive = true;
        MarkAsUpdated(updatedBy);
    }

    /// <summary>
    /// Deactivates the reminder.
    /// </summary>
    public void Deactivate(string updatedBy)
    {
        if (!IsActive)
            throw new InvalidOperationException("Reminder is already inactive.");

        IsActive = false;
        MarkAsUpdated(updatedBy);
    }

    /// <summary>
    /// Updates the recurrence pattern.
    /// </summary>
    public void UpdateRecurrencePattern(string? recurrencePattern, string updatedBy)
    {
        RecurrencePattern = recurrencePattern;
        MarkAsUpdated(updatedBy);
    }

    /// <summary>
    /// Checks if the reminder should trigger now.
    /// </summary>
    public bool ShouldTriggerNow()
    {
        return IsActive && 
               !SentAt.HasValue && 
               ScheduledTime <= DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the reminder is overdue (should have triggered but didn't).
    /// </summary>
    public bool IsOverdue()
    {
        return IsActive && 
               !SentAt.HasValue && 
               ScheduledTime < DateTime.UtcNow.AddMinutes(-5); // 5 minutes grace period
    }
}

/// <summary>
/// Type of reminder.
/// </summary>
public enum ReminderType
{
    /// <summary>
    /// In-app notification.
    /// </summary>
    Notification = 1,

    /// <summary>
    /// Email notification.
    /// </summary>
    Email = 2,

    /// <summary>
    /// SMS notification.
    /// </summary>
    Sms = 3,

    /// <summary>
    /// Push notification.
    /// </summary>
    Push = 4
}