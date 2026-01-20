using System;
using SmartTaskManagementAPI.Domain.Entities.Base;
using SmartTaskManagementAPI.Domain.Enums;

namespace SmartTaskManagementAPI.Domain.Entities;

public class Task : AuditableEntity
{
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public TaskPriority Priority { get; private set; } = TaskPriority.Meduim;
    public TasksStatus Status { get; private set; } = TasksStatus.Draft;
    public DateTime? DueDate { get; private set; } 
    public DateTime? ReminderDate { get; private set; } 

    // Foreign keys
    public Guid TenantId { get; private set; }

    // Navigation property
    public virtual Tenant Tenant { get; private set; } = null!;

    private Task() { }

    public static Task Create(
        string title,
        Guid tenantId,
        Guid createdby,
        TaskPriority priority = TaskPriority.Meduim)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));

        var task = new Task
        {
            Title = title.Trim(),
            TenantId = tenantId,
            Priority = priority,
            Status = TasksStatus.Draft
        };

        task.MarkAsCreated(createdby);
        return task;
    }

    public void Update(
        string title,
        string? description,
        TaskPriority priority,
        DateTime dueDate,
        DateTime? reminderDate,
        Guid updateBy)
    {
        if (Status == TasksStatus.Archived)
            throw new InvalidOperationException("Cannot update an archived task");

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));

        Title = title.Trim();
        Description = description?.Trim();
        Priority = priority;
        DueDate = dueDate;
        ReminderDate = reminderDate;

        MarkAsUpdated(updateBy);
    }

    public void ChangeStatus(TasksStatus newStatus, Guid changeBy)
    {
        if (Status == TasksStatus.Archived && newStatus != TasksStatus.Archived)
            throw new InvalidOperationException("Cannot change status of an archived task");

        if (!Status.CanTransitionTo(newStatus))
            throw new InvalidOperationException($"Cannot transition from {Status} to {newStatus}");

        Status = newStatus;
        MarkAsUpdated(changeBy);
    }

    public void MoveToInProgress(Guid changeBy)
    {
        ChangeStatus(TasksStatus.InProgress, changeBy);
    }

    public void MarkAsDone(Guid changedBy)
    {
        ChangeStatus(TasksStatus.Done, changedBy);
    }

    public void Archive(Guid archivedBy)
    {
        ChangeStatus(TasksStatus.Archived, archivedBy);
    }

    public bool IsOverDue()
    {
        return DueDate.HasValue && DueDate.Value < DateTime.UtcNow && Status != TasksStatus.Done;
    }

    public bool NeedReminder()
    {
        return ReminderDate.HasValue &&
                ReminderDate.Value <= DateTime.UtcNow &&
                Status != TasksStatus.Done &&
                Status != TasksStatus.Archived; 
    }
}
