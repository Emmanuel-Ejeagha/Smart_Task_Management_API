using System;

namespace SmartTaskManagementAPI.Domain.Enums;

public enum TaskStatus
{
    Draft = 1,
    InProgress = 2,
    Done = 3,
    Archived = 4
}

public static class TaskStatusExtentions
{
    public static string GetDisplayName(this TaskStatus status)
    {
        return status switch
        {
            TaskStatus.Draft => "Draft",
            TaskStatus.InProgress => "In Progress",
            TaskStatus.Done => "Done",
            TaskStatus.Archived => "Archived",
            _ => status.ToString()
        };
    }
    
    public static bool CanTransitionTo(this TaskStatus current, TaskStatus next)
    {
        var allowedTransitions = new Dictionary<TaskStatus, List<TaskStatus>>
        {
            { TaskStatus.Draft, new List<TaskStatus> { TaskStatus.InProgress, TaskStatus.Archived}},
            { TaskStatus.InProgress, new List<TaskStatus> { TaskStatus.Done, TaskStatus.Archived }},
            { TaskStatus.Done, new List<TaskStatus> { TaskStatus.Archived}},
            { TaskStatus.Archived, new List<TaskStatus>() }
        };

        return allowedTransitions[current].Contains(next);
    }
}