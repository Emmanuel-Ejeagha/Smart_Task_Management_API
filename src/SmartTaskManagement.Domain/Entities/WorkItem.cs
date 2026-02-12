using SmartTaskManagement.Domain.Entities.Base;
using SmartTaskManagement.Domain.Enums;
using SmartTaskManagement.Domain.Events;

namespace SmartTaskManagement.Domain.Entities;

/// <summary>
/// Main entity representing a task/work item in the system
/// Using WorkItem instead of Task to avoid naming conflicts
/// </summary>
public class WorkItem : AuditableEntity
{
    // Private constructor for EF Core
    private WorkItem() 
    {
        _reminders = new List<Reminder>();
        Tags = new List<string>();
    }

    public WorkItem(
        Guid tenantId,
        string title,
        string? description,
        WorkItemPriority priority,
        DateTime? dueDateUtc,
        string createdBy)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be null or empty", nameof(title));

        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId cannot be empty", nameof(tenantId));

        TenantId = tenantId;
        Title = title;
        Description = description;
        Priority = priority;
        DueDateUtc = dueDateUtc;
        State = WorkItemState.Draft;
        
        MarkAsCreated(createdBy);
        AddDomainEvent(new WorkItemCreatedDomainEvent(this));

        _reminders = new List<Reminder>();
        Tags = new List<string>();
    }

    public Guid TenantId { get; private set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public WorkItemState State { get;  set; }
    public WorkItemPriority Priority { get; set; }
    public DateTime? DueDateUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int EstimatedHours { get; set; }
    public int ActualHours { get; set; }

    // Tags as a simple collection of strings
    public List<string> Tags { get; set; } = new();

    // Navigation properties
    public Tenant? Tenant { get; private set; }

    private readonly List<Reminder> _reminders;
    public IReadOnlyCollection<Reminder> Reminders => _reminders.AsReadOnly();

    /// <summary>
    /// Update basic work item information
    /// Business rule: Archived items cannot be modified
    /// </summary>
    public void Update(
        string title,
        string? description,
        WorkItemPriority priority,
        DateTime? dueDateUtc,
        int estimatedHours,
        string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be null or empty", nameof(title));

        if (State == WorkItemState.Archived)
            throw new InvalidOperationException("Cannot modify an archived work item");

        Title = title;
        Description = description;
        Priority = priority;
        DueDateUtc = dueDateUtc;
        EstimatedHours = estimatedHours;

        MarkAsUpdated(updatedBy);
    }

    /// <summary>
    /// Transition to InProgress state
    /// </summary>
    public void Start(string updatedBy)
    {
        if (State == WorkItemState.Archived)
            throw new InvalidOperationException("Cannot start an archived work item");

        if (State == WorkItemState.InProgress)
            return;

        var previousState = State;
        State = WorkItemState.InProgress;

        MarkAsUpdated(updatedBy);
        AddDomainEvent(new WorkItemStateChangedDomainEvent(this, previousState, State));
    }

    /// <summary>
    /// Transition to Completed state
    /// </summary>
    public void Complete(string updatedBy, int actualHours = 0)
    {
        if (State == WorkItemState.Archived)
            throw new InvalidOperationException("Cannot complete an archived work item");

        if (State == WorkItemState.Completed)
            return;

        var previousState = State;
        State = WorkItemState.Completed;
        CompletedAtUtc = DateTime.UtcNow;
        ActualHours = actualHours;

        MarkAsUpdated(updatedBy);
        AddDomainEvent(new WorkItemStateChangedDomainEvent(this, previousState, State));

        // Cancel any pending reminders
        foreach (var reminder in _reminders.Where(r => r.IsPending()))
        {
            reminder.Cancel(updatedBy);
        }
    }

    /// <summary>
    /// Transition to Archived state
    /// Business rule: Archived items cannot be modified
    /// </summary>
    public void Archive(string updatedBy)
    {
        if (State == WorkItemState.Archived)
            return;

        var previousState = State;
        State = WorkItemState.Archived;

        MarkAsUpdated(updatedBy);
        AddDomainEvent(new WorkItemStateChangedDomainEvent(this, previousState, State));

        // Cancel all reminders when archiving
        foreach (var reminder in _reminders.Where(r => r.IsPending()))
        {
            reminder.Cancel(updatedBy);
        }
    }

    /// <summary>
    /// Move back to Draft state
    /// </summary>
    public void Reopen(string updatedBy)
    {
        if (State == WorkItemState.Archived)
            throw new InvalidOperationException("Cannot reopen an archived work item");

        if (State == WorkItemState.Draft)
            return;

        var previousState = State;
        State = WorkItemState.Draft;
        CompletedAtUtc = null;

        MarkAsUpdated(updatedBy);
        AddDomainEvent(new WorkItemStateChangedDomainEvent(this, previousState, State));
    }

    /// <summary>
    /// Add a tag to the work item
    /// </summary>
    public void AddTag(string tag, string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Tag cannot be null or empty", nameof(tag));

        if (State == WorkItemState.Archived)
            throw new InvalidOperationException("Cannot modify an archived work item");

        if (Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
            return;

        Tags.Add(tag);
        MarkAsUpdated(updatedBy);
    }

    /// <summary>
    /// Remove a tag from the work item
    /// </summary>
    public void RemoveTag(string tag, string updatedBy)
    {
        if (State == WorkItemState.Archived)
            throw new InvalidOperationException("Cannot modify an archived work item");

        Tags.RemoveAll(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase));
        MarkAsUpdated(updatedBy);
    }

    /// <summary>
    /// Add a reminder to the work item
    /// </summary>
    public void AddReminder(Reminder reminder, string updatedBy)
    {
        if (reminder is null)
            throw new ArgumentNullException(nameof(reminder));

        if (State == WorkItemState.Archived)
            throw new InvalidOperationException("Cannot add reminders to an archived work item");

        if (_reminders.Any(r => r.Id == reminder.Id))
            throw new InvalidOperationException("Reminder already exists");

        _reminders.Add(reminder);
        MarkAsUpdated(updatedBy);
        AddDomainEvent(new ReminderScheduledDomainEvent(reminder));
    }

    /// <summary>
    /// Remove a reminder from the work item
    /// </summary>
    public void RemoveReminder(Guid reminderId, string updatedBy)
    {
        if (State == WorkItemState.Archived)
            throw new InvalidOperationException("Cannot modify an archived work item");

        var reminder = _reminders.FirstOrDefault(r => r.Id == reminderId);
        if (reminder is not null)
        {
            _reminders.Remove(reminder);
            MarkAsUpdated(updatedBy);
        }
    }

    /// <summary>
    /// Override soft delete to check business rules
    /// Business rule: Only admins can delete work items (enforced in Application layer)
    /// </summary>
    public override void MarkAsDeleted(string deletedBy)
    {
        if (State == WorkItemState.Archived)
            throw new InvalidOperationException("Cannot delete an archived work item");

        base.MarkAsDeleted(deletedBy);
    }

    /// <summary>
    /// Calculate if the work item is overdue
    /// </summary>
    public bool IsOverdue()
    {
        return DueDateUtc.HasValue 
            && DueDateUtc.Value < DateTime.UtcNow 
            && State != WorkItemState.Completed 
            && State != WorkItemState.Archived 
            && State != WorkItemState.Cancelled;
    }
}