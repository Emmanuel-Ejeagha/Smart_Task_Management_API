using System;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartTaskManagementAPI.Application.Common.Exceptions;
using SmartTaskManagementAPI.Application.Common.Interfaces;
using SmartTaskManagementAPI.Application.Features.Tasks.DTOs;
using SmartTaskManagementAPI.Application.Interfaces;

namespace SmartTaskManagementAPI.Application.Features.Tasks.Queries.GetTaskById;

public class GetTaskByIdQueryHandler : IRequestHandler<GetTaskByIdQuery, TaskDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantAccessChecker _tenantAccessChecker;
    private readonly IMapper _mapper;
    private readonly ILogger<GetTaskByIdQueryHandler> _logger;
    public GetTaskByIdQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ITenantAccessChecker tenantAccessChecker,
        IMapper mapper,
        ILogger<GetTaskByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _tenantAccessChecker = tenantAccessChecker;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<TaskDto> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Get current user info
            if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
                throw new UnauthorizedAccessException("User not authenticated");

            // Get the task
            var task = await _unitOfWork.Tasks.GetByIdAsync(request.TaskId, cancellationToken);
            if (task == null || task.IsDeleted)
                throw new NotFoundException("Task", request.TaskId);

            // Check tenant isolation
            await _tenantAccessChecker.CheckTaskAccessAsync(task, currentUserId, cancellationToken);

            // map to DTO and return
            var taskDto = _mapper.Map<TaskDto>(task);

            _logger.LogInformation("Task retrieved successfully. TaskId: {TaskId}, UserId: {userId}",
                task.Id, currentUserId);

            return taskDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving task {TaskId} for user: {UserId}",
                request.TaskId, _currentUserService.UserId);
            throw;
        }
    }
}
