using System;
using SmartTaskManagementAPI.Domain.Enums;

namespace SmartTaskManagementAPI.Application.Features.Tasks.DTOs;

public class TaskListDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public TaskPriority Priority { get; set; }
    public string PriorityDisplay { get; set; } = string.Empty;
    public TasksStatus Status { get; set; }
    public string StatusDisplay { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public bool IsOverdue { get; set; }
}
