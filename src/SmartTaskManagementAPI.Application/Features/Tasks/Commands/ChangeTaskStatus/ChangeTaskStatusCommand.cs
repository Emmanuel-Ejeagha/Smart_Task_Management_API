using System;
using MediatR;
using SmartTaskManagementAPI.Application.Features.Tasks.DTOs;
using SmartTaskManagementAPI.Domain.Enums;

namespace SmartTaskManagementAPI.Application.Features.Tasks.Commands.ChangeTaskStatus;

public class ChangeTaskStatusCommand : IRequest<TaskDto>
{
    public Guid TaskId { get; set; }
    public TasksStatus NewStatus { get; set; }
}
