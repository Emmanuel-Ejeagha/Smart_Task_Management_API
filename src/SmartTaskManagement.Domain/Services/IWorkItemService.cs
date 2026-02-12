using SmartTaskManagement.Domain.Entities;

namespace SmartTaskManagement.Domain.Services;

/// <summary>
/// Domain service interface for work item operations that involve multiple aggregates
/// </summary>
public interface IWorkItemService
{
    /// <summary>
    /// Checks if a work item can be transitioned to a new state
    /// </summary>
    bool CanTransitionToState(WorkItem workItem, Enums.WorkItemState newState);

    /// <summary>
    /// Calculates the progress percentage of a work item based on estimated vs actual hours
    /// </summary>
    int CalculateProgressPercentage(WorkItem workItem);

    /// <summary>
    /// Validates if a reminder can be scheduled for a work item
    /// </summary>
    bool CanScheduleReminder(WorkItem workItem, DateTime reminderDateUtc);
}