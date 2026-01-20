using System;

namespace SmartTaskManagementAPI.Domain.Enums;

public enum TasksStatus
{
    Draft = 1,
    InProgress = 2,
    Done = 3,
    Archived = 4
}

public static class TaskStatusExtentions
{
    public static string GetDisplayName(this TasksStatus status)
    {
        return status switch
        {
            TasksStatus.Draft => "Draft",
            TasksStatus.InProgress => "In Progress",
            TasksStatus.Done => "Done",
            TasksStatus.Archived => "Archived",
            _ => status.ToString()
        };
    }
    
    public static bool CanTransitionTo(this TasksStatus current, TasksStatus next)
    {
        var allowedTransitions = new Dictionary<TasksStatus, List<TasksStatus>>
        {
            { TasksStatus.Draft, new List<TasksStatus> { TasksStatus.InProgress, TasksStatus.Archived}},
            { TasksStatus.InProgress, new List<TasksStatus> { TasksStatus.Done, TasksStatus.Archived }},
            { TasksStatus.Done, new List<TasksStatus> { TasksStatus.Archived}},
            { TasksStatus.Archived, new List<TasksStatus>() }
        };

        return allowedTransitions[current].Contains(next);
    }
}