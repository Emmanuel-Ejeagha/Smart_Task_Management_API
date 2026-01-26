using System;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartTaskManagementAPI.Application.Common.Exceptions;
using SmartTaskManagementAPI.Application.Common.Interfaces;
using SmartTaskManagementAPI.Application.Features.Tasks.DTOs;
using SmartTaskManagementAPI.Application.Interfaces;

namespace SmartTaskManagementAPI.Application.Features.Tasks.Commands.ChangeTaskStatus;

public class ChangeTaskStatusCommandHandler : IRequestHandler<ChangeTaskStatusCommand, TaskDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly ITenantAccessChecker _tenantAccessChecker;
    private readonly ILogger<ChangeTaskStatusCommandHandler> _logger;

    public ChangeTaskStatusCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMapper mapper,
        ITenantAccessChecker tenantAccessChecker,
        ILogger<ChangeTaskStatusCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _tenantAccessChecker = tenantAccessChecker;
        _logger = logger;
    }

    public async Task<TaskDto> Handle(ChangeTaskStatusCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get current user info 
            if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
                throw new UnauthorizedAccessException("User is not authenticated");

            // Get current user info
            var task = await _unitOfWork.Tasks.GetByIdAsync(request.TaskId, cancellationToken);
            if (task == null || task.IsDeleted)
                throw new NotFoundException("Task", request.TaskId);

            // check tenant Isolation
            await _tenantAccessChecker.CheckTaskAccessAsync(task, currentUserId, cancellationToken);

            // Change status using domain entity method
            var oldStatus = task.Status;

            task.ChangeStatus(request.NewStatus, currentUserId);

            // Save changes
            _unitOfWork.Tasks.Update(task);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Map to DTO and return
            var taskDto = _mapper.Map<TaskDto>(task);

            _logger.LogInformation("Task status changed successfully. TaskId: {TaskId}, OldStatus: {OldStatus}, NewStatus: {NewStatus}, UserId: {UserId}",
                task.Id, oldStatus, request.NewStatus, currentUserId);


            return taskDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing task status for task {TaskId} by user: {UserId}",
                request.TaskId, _currentUserService.UserId);
            throw;
        }
    }
}
