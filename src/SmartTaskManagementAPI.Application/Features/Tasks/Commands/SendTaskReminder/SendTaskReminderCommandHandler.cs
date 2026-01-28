using MediatR;
using Microsoft.Extensions.Logging;
using SmartTaskManagementAPI.Application.Common.Interfaces;
using SmartTaskManagementAPI.Application.Interfaces;

namespace SmartTaskManagementAPI.Application.Features.Tasks.Commands.SendTaskReminder;

public class SendTaskReminderCommandHandler : IRequestHandler<SendTaskReminderCommand, Unit>
{
    private readonly ILogger<SendTaskReminderCommandHandler> _logger;
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;
    private readonly ITaskRepository _taskRepository;

    public SendTaskReminderCommandHandler(
        ILogger<SendTaskReminderCommandHandler> logger,
        IEmailService emailService,
        IUserRepository userRepository,
        ITaskRepository taskRepository)
    {
        _logger = logger;
        _emailService = emailService;
        _userRepository = userRepository;
        _taskRepository = taskRepository;
    }

    public async Task<Unit> Handle(SendTaskReminderCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get task and user
            var task = await _taskRepository.GetByIdAsync(request.TaskId, cancellationToken);
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            
            if (task == null || task.IsDeleted)
            {
                _logger.LogWarning("Task not found for reminder: {TaskId}", request.TaskId);
                return Unit.Value;
            }
            
            if (user == null || user.IsDeleted || !user.IsActive)
            {
                _logger.LogWarning("User not found or inactive for reminder: {UserId}", request.UserId);
                return Unit.Value;
            }
            
            // Check if task still needs reminder
            if (!task.NeedsReminder())
            {
                _logger.LogInformation("Task no longer needs reminder: {TaskId}", request.TaskId);
                return Unit.Value;
            }
            
            // Send reminder email
            await _emailService.SendTaskReminderEmailAsync(
                user.Email,
                user.GetFullName(),
                task.Title,
                task.DueDate ?? DateTime.UtcNow,
                cancellationToken);
            
            _logger.LogInformation(
                "Immediate reminder sent for task: {TaskId}, User: {UserEmail}",
                task.Id, user.Email);
            
            return Unit.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending immediate task reminder for task {TaskId}", request.TaskId);
            throw;
        }
    }
}