using System;
using MediatR;

namespace SmartTaskManagementAPI.Application.Features.Tasks.Commands.DeleteTask;

public class DeleteTaskCommand : IRequest<Unit>
{
    public Guid TaskId { get; set; }
}
