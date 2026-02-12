using SmartTaskManagement.Domain.Enums;

namespace SmartTaskManagement.Application.Features.Reminders.Dtos;

public class ReminderDto
{
    public Guid Id { get; set; }
    public Guid WorkItemId { get; set; }
    public string WorkItemTitle { get; set; } = string.Empty;
    public DateTime ReminderDateUtc { get; set; }
    public string Message { get; set; } = string.Empty;
    public ReminderStatus Status { get; set; }
    public DateTime? TriggeredAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsPending { get; set; }
    public bool IsDue { get; set; }
}

public class TriggerReminderRequest
{
    public string? ErrorMessage { get; set; }
}

public class RescheduleReminderRequest
{
    public DateTime NewReminderDateUtc { get; set; }
    public string? Message { get; set; }
}