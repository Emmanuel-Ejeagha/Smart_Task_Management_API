using System;

namespace SmartTaskManagementAPI.Application.Features.TasksV2;

public class TaskStatisticsDto
{
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int OverdueTasks { get; set; }
    public int DueThisWeek { get; set; }
    public int HighPriorityTasks { get; set; }
    public double CompletionRate { get; set; }
    public Dictionary<string, int> TasksByPriority { get; set; } = new();
    public Dictionary<string, int> TasksByStatus { get; set; } = new();
}