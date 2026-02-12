using SmartTaskManagement.Domain.Enums;

namespace SmartTaskManagement.Application.Features.WorkItems.Dtos;

public class WorkItemDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public WorkItemState State { get; set; }
    public WorkItemPriority Priority { get; set; }
    public DateTime? DueDateUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int EstimatedHours { get; set; }
    public int ActualHours { get; set; }
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsOverdue { get; set; }
    public int ProgressPercentage { get; set; }
}

public class CreateWorkItemRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public WorkItemPriority Priority { get; set; } = WorkItemPriority.Medium;
    public DateTime? DueDateUtc { get; set; }
    public int EstimatedHours { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class UpdateWorkItemRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public WorkItemPriority Priority { get; set; }
    public DateTime? DueDateUtc { get; set; }
    public int EstimatedHours { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class ChangeWorkItemStateRequest
{
    public WorkItemState NewState { get; set; }
    public int? ActualHours { get; set; }
}

public class AddReminderRequest
{
    public DateTime ReminderDateUtc { get; set; }
    public string Message { get; set; } = string.Empty;
}