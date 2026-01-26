using System;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartTaskManagementAPI.Application.Common.Exceptions;
using SmartTaskManagementAPI.Application.Common.Interfaces;
using SmartTaskManagementAPI.Application.Interfaces;

namespace SmartTaskManagementAPI.Application.Features.Tasks.Commands.DeleteTask;

public class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private ILogger<DeleteTaskCommandHandler> _logger;
    private ITenantAccessChecker _tenantAccessChecker;

    public DeleteTaskCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<DeleteTaskCommandHandler> logger,
        ITenantAccessChecker tenantAccessChecker)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
        _tenantAccessChecker = tenantAccessChecker;
    }

    public async Task<Unit> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get current user info 
            if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
                throw new UnauthorizedAccessException("User not authenticated");

            //  Check if user is admin
            if (!_currentUserService.IsInRole("Admin"))
                throw new UnauthorizedAccessException("Only administrators can delete tasks");

            // Get the task
            var task = await _unitOfWork.Tasks.GetByIdAsync(request.TaskId, cancellationToken);
            if (task == null || task.IsDeleted)
                throw new NotFoundException("Task", request.TaskId);

            //  check tenant isolation
            await _tenantAccessChecker.CheckTaskAccessAsync(task, currentUserId, cancellationToken);

            // Perform soft delete using domain entity method
            task.MarkAsDeleted(currentUserId);

            // Save changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Task soft deleted successfully. TaskId: {TaskId}, UserId: {userId}",
                task.Id, currentUserId);

            return Unit.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting task {taskId} for user: {UserId}",
                request.TaskId, _currentUserService.UserId);
            throw;
        }
    }
}
