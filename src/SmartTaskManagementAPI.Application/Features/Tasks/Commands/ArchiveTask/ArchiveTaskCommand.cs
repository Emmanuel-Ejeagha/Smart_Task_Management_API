using System;
using MediatR;
using SmartTaskManagementAPI.Application.Features.Tasks.DTOs;

namespace SmartTaskManagementAPI.Application.Features.Tasks.Commands.ArchiveTask;

public class ArchiveTaskCommand : IRequest<TaskDto>
{
    public Guid TaskId { get; set; }
}