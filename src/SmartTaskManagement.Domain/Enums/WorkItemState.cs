namespace SmartTaskManagement.Domain.Enums;

/// <summary>
/// Represents the state of a WorkItem
/// Using WorkItemState instead of TaskStatus to avoid naming conflicts
/// </summary>
public enum WorkItemState
{
    /// <summary>
    /// WorkItem is in draft state, not yet active
    /// </summary>
    Draft = 0,
    
    /// <summary>
    /// WorkItem is currently being worked on
    /// </summary>
    InProgress = 1,
    
    /// <summary>
    /// WorkItem has been completed
    /// </summary>
    Completed = 2,
    
    /// <summary>
    /// WorkItem is archived and cannot be modified
    /// Business rule: Archived items cannot be modified
    /// </summary>
    Archived = 3,
    
    /// <summary>
    /// WorkItem is on hold, temporarily inactive
    /// </summary>
    OnHold = 4,
    
    /// <summary>
    /// WorkItem has been cancelled
    /// </summary>
    Cancelled = 5
}