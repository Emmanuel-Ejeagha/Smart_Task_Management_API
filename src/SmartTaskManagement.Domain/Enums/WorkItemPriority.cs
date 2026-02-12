namespace SmartTaskManagement.Domain.Enums;

/// <summary>
/// Represents the priority of a WorkItem
/// </summary>
public enum WorkItemPriority
{
    /// <summary>
    /// Low priority, can be done later
    /// </summary>
    Low = 0,
    
    /// <summary>
    /// Normal/default priority
    /// </summary>
    Medium = 1,
    
    /// <summary>
    /// High priority, should be done soon
    /// </summary>
    High = 2,
    
    /// <summary>
    /// Critical priority, must be done immediately
    /// </summary>
    Critical = 3
}