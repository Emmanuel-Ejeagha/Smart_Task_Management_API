using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartTaskManagementAPI.Application.Common.Exceptions;
using SmartTaskManagementAPI.Application.Common.Interfaces;
using SmartTaskManagementAPI.Application.Features.Tasks.DTOs;
using SmartTaskManagementAPI.Domain.Enums;

namespace SmartTaskManagementAPI.Application.Features.Tasks.Commands.UpdateTask;

public class UpdateTaskCommandHandler : IRequestHandler<UpdateTaskCommand, TaskDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateTaskCommandHandler> _logger;

    public UpdateTaskCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMapper mapper,
        ILogger<UpdateTaskCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<TaskDto> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
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
            await CheckTenantAccess(task, currentUserId, cancellationToken);

            // Check if task is archived (business rule)
            if (task.Status == TasksStatus.Archived)
                throw new InvalidOperationException("Cannot update an archived task");

            // Update the task using domain entity method
            task.Update(
                request.Title,
                request.Description,
                request.Priority,
                request.DueDate,
                request.ReminderDate,
                currentUserId);

            // Save changes
            _unitOfWork.Tasks.Update(task);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Map to DTO and return
            var taskDto = _mapper.Map<TaskDto>(task);
            
            _logger.LogInformation("Task updated successfully. TaskId: {TaskId}, UserId: {UserId}", 
                task.Id, currentUserId);
            
            return taskDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating task {TaskId} for user: {UserId}", 
                request.TaskId, _currentUserService.UserId);
            throw;
        }
    }

    private async Task CheckTenantAccess(Domain.Entities.Task task, Guid userId, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.User.GetByIdAsync(userId, cancellationToken);
        if (user == null || user.IsDeleted)
            throw new NotFoundException("User", userId);

        if (user.TenantId != task.TenantId)
            throw new UnauthorizedAccessException("Access denied to task from different tenant");
    }
}