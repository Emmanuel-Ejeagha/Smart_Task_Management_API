using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartTaskManagementAPI.Application.Common.Exceptions;
using SmartTaskManagementAPI.Application.Common.Interfaces;
using SmartTaskManagementAPI.Application.Features.Tasks.DTOs;

namespace SmartTaskManagementAPI.Application.Features.Tasks.Commands.CreateTask;

public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, TaskDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateTaskCommandHandler> _logger;
    private readonly IJobScheduler _jobScheduler;

    public CreateTaskCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMapper mapper,
        ILogger<CreateTaskCommandHandler> logger,
        IJobScheduler jobScheduler)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _logger = logger;
        _jobScheduler = jobScheduler;
    }

    public async Task<TaskDto> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get current user info
            if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
                throw new UnauthorizedAccessException("User not authenticated");

            // Get current user to get tenant ID
            var currentUser = await _unitOfWork.User.GetByIdAsync(currentUserId, cancellationToken);
            if (currentUser == null || currentUser.IsDeleted)
                throw new NotFoundException("User", currentUserId);

            if (!currentUser.IsActive)
                throw new UnauthorizedAccessException("User account is deactivated");

            // Create domain task entity
            var task = Domain.Entities.Task.Create(
                request.Title,
                currentUser.TenantId,
                currentUserId,
                request.Priority);

            // Update additional properties if provided
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

            // Add to repository
            await _unitOfWork.Tasks.AddAsync(task, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Schedule reminder if reminder date is set
            if (request.ReminderDate.HasValue && request.ReminderDate > DateTime.UtcNow)
            {
                var jobId = _jobScheduler.ScheduleTaskReminder(
                    task.Id, 
                    currentUserId, 
                    request.ReminderDate.Value);
                
                _logger.LogInformation(
                    "Scheduled reminder job {JobId} for task {TaskId} at {ReminderDate}",
                    jobId, task.Id, request.ReminderDate);
            }

            // Map to DTO and return
            var taskDto = _mapper.Map<TaskDto>(task);
            
            _logger.LogInformation("Task created successfully. TaskId: {TaskId}, UserId: {UserId}", 
                task.Id, currentUserId);
            
            return taskDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating task for user: {UserId}", _currentUserService.UserId);
            throw;
        }
    }
}