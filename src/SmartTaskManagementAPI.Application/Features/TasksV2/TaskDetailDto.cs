using System;
using MediatR;
using SmartTaskManagementAPI.Application.Features.Tasks.Commands.CreateTask;
using SmartTaskManagementAPI.Application.Features.Tasks.DTOs;

namespace SmartTaskManagementAPI.Application.Features.TasksV2;

public class TaskDetailDto : TaskDto
{
    public bool IsOverdue { get; set; }
    public int? DaysUntilDue { get; set; }
    public bool CanBeArchived { get; set; }
    public bool CanTransitionToInProgress { get; set; }
    public bool CanTransitionToDone { get; set; }
}

public class CreateTaskV2Command : CreateTaskCommand
{
    public List<string>? Tags { get; set; }
    public Guid? AssigneeId { get; set; }
}

public class BulkUpdateTaskStatusCommand : IRequest<Unit>
{
    public List<Guid> TaskIds { get; set; } = new();
    public TaskStatus NewStatus { get; set; }
}

public class GetTaskStatisticsQuery : IRequest<TaskStatisticsDto>
{
}
