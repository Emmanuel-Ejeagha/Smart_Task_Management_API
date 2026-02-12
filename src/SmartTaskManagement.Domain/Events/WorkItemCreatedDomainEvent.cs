using SmartTaskManagement.Domain.Entities;

namespace SmartTaskManagement.Domain.Events;

/// <summary>
/// Domain event raised when a work item is created
/// </summary>
public sealed class WorkItemCreatedDomainEvent
{
    public WorkItemCreatedDomainEvent(WorkItem workItem)
    {
        WorkItemId = workItem.Id;
        TenantId = workItem.TenantId;
        Title = workItem.Title;
        CreatedBy = workItem.CreatedBy;
        CreatedAtUtc = workItem.CreatedAtUtc;
    }

    public Guid WorkItemId { get; }
    public Guid TenantId { get; }
    public string Title { get; }
    public string CreatedBy { get; }
    public DateTime CreatedAtUtc { get; }
}