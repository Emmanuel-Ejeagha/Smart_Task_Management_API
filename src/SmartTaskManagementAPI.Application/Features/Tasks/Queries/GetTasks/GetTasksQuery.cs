using System;
using MediatR;
using SmartTaskManagementAPI.Application.Common.Models;
using SmartTaskManagementAPI.Application.Features.Tasks.DTOs;
using SmartTaskManagementAPI.Domain.Enums;

namespace SmartTaskManagementAPI.Application.Features.Tasks.Queries.GetTasks;

public class GetTasksQuery : IRequest<PaginatedResult<TaskListDto>>
{
    public PaginationQuery Pagination { get; set; } = new PaginationQuery();
    public TasksStatus? Status { get; set; }
    public TaskPriority? Priority { get; set; }
    public bool? OverDueOnly { get; set; }
}
