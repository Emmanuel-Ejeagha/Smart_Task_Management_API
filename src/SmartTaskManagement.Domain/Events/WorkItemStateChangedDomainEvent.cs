namespace SmartTaskManagement.Domain.Events;

/// <summary>
/// Domain event raised when a work item's state changes
/// </summary>
public sealed class WorkItemStateChangedDomainEvent
{
    public WorkItemStateChangedDomainEvent(
        Entities.WorkItem workItem,
        Enums.WorkItemState previousState,
        Enums.WorkItemState newState)
    {
        WorkItemId = workItem.Id;
        TenantId = workItem.TenantId;
        PreviousState = previousState;
        NewState = newState;
        ChangedBy = workItem.UpdatedBy ?? workItem.CreatedBy;
        ChangedAtUtc = workItem.UpdatedAtUtc ?? workItem.CreatedAtUtc;
    }

    public Guid WorkItemId { get; }
    public Guid TenantId { get; }
    public Enums.WorkItemState PreviousState { get; }
    public Enums.WorkItemState NewState { get; }
    public string ChangedBy { get; }
    public DateTime ChangedAtUtc { get; }
}