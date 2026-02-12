using SmartTaskManagement.Domain.Entities;

namespace SmartTaskManagement.Domain.Services;

/// <summary>
/// Domain service implementation for work item operations
/// Contains business logic that doesn't naturally fit in the WorkItem entity
/// </summary>
public class WorkItemDomainService : IWorkItemService
{
    public bool CanTransitionToState(WorkItem workItem, Enums.WorkItemState newState)
    {
        if (workItem.State == Enums.WorkItemState.Archived)
        {
            // Archived items cannot transition to any other state
            return false;
        }

        // Define allowed state transitions
        var allowedTransitions = new Dictionary<Enums.WorkItemState, List<Enums.WorkItemState>>
        {
            [Enums.WorkItemState.Draft] = new List<Enums.WorkItemState>
            {
                Enums.WorkItemState.InProgress,
                Enums.WorkItemState.OnHold,
                Enums.WorkItemState.Cancelled,
                Enums.WorkItemState.Archived
            },
            [Enums.WorkItemState.InProgress] = new List<Enums.WorkItemState>
            {
                Enums.WorkItemState.Completed,
                Enums.WorkItemState.OnHold,
                Enums.WorkItemState.Cancelled,
                Enums.WorkItemState.Archived,
                Enums.WorkItemState.Draft // Allow reopening
            },
            [Enums.WorkItemState.Completed] = new List<Enums.WorkItemState>
            {
                Enums.WorkItemState.Archived,
                Enums.WorkItemState.Draft, // Allow reopening
                Enums.WorkItemState.InProgress // Allow restarting
            },
            [Enums.WorkItemState.OnHold] = new List<Enums.WorkItemState>
            {
                Enums.WorkItemState.InProgress,
                Enums.WorkItemState.Cancelled,
                Enums.WorkItemState.Archived
            },
            [Enums.WorkItemState.Cancelled] = new List<Enums.WorkItemState>
            {
                Enums.WorkItemState.Draft, // Allow reopening
                Enums.WorkItemState.Archived
            }
        };

        return allowedTransitions.TryGetValue(workItem.State, out var allowedStates) 
            && allowedStates.Contains(newState);
    }

    public int CalculateProgressPercentage(WorkItem workItem)
    {
        if (workItem.EstimatedHours <= 0)
            return 0;

        if (workItem.State == Enums.WorkItemState.Completed)
            return 100;

        // Calculate progress based on actual hours vs estimated hours
        // Cap at 100% even if actual hours exceed estimated
        var percentage = (int)Math.Min(100, (workItem.ActualHours * 100.0) / workItem.EstimatedHours);

        // Ensure minimum progress for active items
        if (workItem.State == Enums.WorkItemState.InProgress && percentage == 0)
            return 1;

        return percentage;
    }

    public bool CanScheduleReminder(WorkItem workItem, DateTime reminderDateUtc)
    {
        // Can't schedule reminders for archived or cancelled items
        if (workItem.State == Enums.WorkItemState.Archived || 
            workItem.State == Enums.WorkItemState.Cancelled)
            return false;

        // Reminder must be in the future
        if (reminderDateUtc <= DateTime.UtcNow)
            return false;

        // If work item has a due date, reminder must be before due date
        if (workItem.DueDateUtc.HasValue && reminderDateUtc > workItem.DueDateUtc.Value)
            return false;

        // Check if there are too many active reminders (max 5 per work item)
        var activeReminders = workItem.Reminders.Count(r => 
            r.Status == Enums.ReminderStatus.Scheduled);
        
        return activeReminders < 5;
    }
}