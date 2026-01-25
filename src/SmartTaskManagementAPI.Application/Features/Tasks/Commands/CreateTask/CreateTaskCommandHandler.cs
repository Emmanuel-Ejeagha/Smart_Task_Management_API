using System;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartTaskManagementAPI.Application.Common.Interfaces;
using SmartTaskManagementAPI.Application.Features.Tasks.DTOs;
using TaskEntity = SmartTaskManagementAPI.Domain.Entities.Task;

namespace SmartTaskManagementAPI.Application.Features.Tasks.Commands.CreateTask;

public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, TaskDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateTaskCommandHandler> _logger;
    public CreateTaskCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMapper mapper,
        ILogger<CreateTaskCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<TaskDto> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
                throw new UnauthorizedAccessException("User not authenticated");

            if (string.IsNullOrEmpty(_currentUserService.UserId))
                throw new UnauthorizedAccessException("User ID not found in claims");

            // Get user to get tenant ID
            var user = await _unitOfWork.User.GetByIdAsync(currentUserId, cancellationToken);
            if (user == null || user.IsDeleted)
                throw new UnauthorizedAccessException("User not found or inactive");

            // Create the task
            var task = TaskEntity.Create(
                request.Title,
                user.TenantId,
                currentUserId,
                request.Priority);

            // Update optional fields
            if (!string.IsNullOrWhiteSpace(request.Description) ||
                request.DueDate.HasValue ||
                request.ReminderDate.HasValue)
            {
                task.Update(
                    request.Title,
                    request.Description,
                    request.Priority,
                    request.DueDate,
                    request.ReminderDate,
                    currentUserId);
            }

            await _unitOfWork.Tasks.AddAsync(task, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Task created: {TaskId} by user {UserId}", task.Id, currentUserId);

            return _mapper.Map<TaskDto>(task);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating task");
            throw;
        }
    }
}
