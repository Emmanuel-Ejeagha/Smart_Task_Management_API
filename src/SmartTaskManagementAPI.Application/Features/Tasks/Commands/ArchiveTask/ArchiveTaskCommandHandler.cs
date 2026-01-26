using System;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartTaskManagementAPI.Application.Common.Exceptions;
using SmartTaskManagementAPI.Application.Common.Interfaces;
using SmartTaskManagementAPI.Application.Features.Tasks.DTOs;
using SmartTaskManagementAPI.Application.Interfaces;

namespace SmartTaskManagementAPI.Application.Features.Tasks.Commands.ArchiveTask;

public class ArchiveTaskCommandHandler : IRequestHandler<ArchiveTaskCommand, TaskDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly ITenantAccessChecker _tenantAccessChecker;
    private readonly ILogger _logger;

    public ArchiveTaskCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMapper mapper,
        ITenantAccessChecker tenantAccessChecker,
        ILogger<ArchiveTaskCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _tenantAccessChecker = tenantAccessChecker;
        _logger = logger;
    }

    public async Task<TaskDto> Handle(ArchiveTaskCommand request, CancellationToken cancellationToken)
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

            // Archive the task using domainentity method
            task.Archive(currentUserId);

            // Save changes
            _unitOfWork.Tasks.Update(task);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Map to DTO and return
            var taskDto = _mapper.Map<TaskDto>(task);

            _logger.LogInformation("Task archived successfully. TaskId: {TaskId}, UserId: {UserId}",
                task.Id, currentUserId);

            return taskDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving task {TaskId} for user: {UserId}",
                request.TaskId, _currentUserService.UserId);
            throw;
        }
    }
}
