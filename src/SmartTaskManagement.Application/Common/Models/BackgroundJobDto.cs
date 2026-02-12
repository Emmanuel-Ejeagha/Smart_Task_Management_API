namespace SmartTaskManagement.Application.Common.Models;

/// <summary>
/// DTO for background job information
/// </summary>
public class BackgroundJobDto
{
    public string JobId { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
    public DateTime? ScheduledAt { get; set; }
    public DateTime? EnqueuedAt { get; set; }
    public DateTime? ExecutedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Error { get; set; }
    public object? JobData { get; set; }
}

/// <summary>
/// Request for scheduling a background job
/// </summary>
public class ScheduleJobRequest
{
    public string JobType { get; set; } = string.Empty;
    public object JobData { get; set; } = new();
    public DateTime ScheduleAt { get; set; }
    public string? RecurringCronExpression { get; set; }
}