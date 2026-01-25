namespace SmartTaskManagementAPI.Domain.Enums;

public enum TaskPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public static class TaskPrioryExxtensions
{
    public static string GetDisplayName(this TaskPriority priority)
    {
        return priority switch
        {
            TaskPriority.Low => "Low",
            TaskPriority.Medium => "Medium",
            TaskPriority.High => "High",
            TaskPriority.Critical => "Critical",
            _ => priority.ToString()
        };
    }
    
    public static int GetWeight(this TaskPriority priority)
    {
        return priority switch
        {
            TaskPriority.Low => 1,
            TaskPriority.Medium => 2,
            TaskPriority.High => 3,
            TaskPriority.Critical => 4,
            _ => 0
        };
    }
}