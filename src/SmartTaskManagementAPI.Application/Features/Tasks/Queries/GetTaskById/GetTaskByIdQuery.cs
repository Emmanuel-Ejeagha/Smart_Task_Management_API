using System;
using MediatR;
using SmartTaskManagementAPI.Application.Features.Tasks.DTOs;

namespace SmartTaskManagementAPI.Application.Features.Tasks.Queries.GetTaskById;

public class GetTaskByIdQuery : IRequest<TaskDto>
{
    public Guid TaskId { get; set; }
}
