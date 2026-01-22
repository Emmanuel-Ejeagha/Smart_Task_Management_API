using System;
using SmartTaskManagementAPI.Domain.Enums;

namespace SmartTaskManagementAPI.Application.Features.Tasks.DTOs;

public class TaskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskPriority Priority { get; set; }
    public string PriorityDisplay { get; set; } = string.Empty;
    public TasksStatus Status { get; set; }
    public string StatusDisplay { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public DateTime? ReminderDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
