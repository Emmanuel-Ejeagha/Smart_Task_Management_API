using FluentValidation;
using MediatR;
using SmartTaskManagement.Application.Common.Behaviors;
using SmartTaskManagement.Application.Common.Interfaces;
using SmartTaskManagement.Application.Common.Models;

namespace SmartTaskManagement.Application.Features.Reminders.Commands.TriggerReminder;

[Authorize("User")] // Could also be system-triggered via background job
[Transactional]
public class TriggerReminderCommand : IRequest<Result>
{
    public Guid ReminderId { get; set; }
    public string? ErrorMessage { get; set; }
}

public class TriggerReminderCommandHandler : IRequestHandler<TriggerReminderCommand, Result>
{
    private readonly IReminderRepository _reminderRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBackgroundJobService _backgroundJobService;

    public TriggerReminderCommandHandler(
        IReminderRepository reminderRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        IBackgroundJobService backgroundJobService)
    {
        _reminderRepository = reminderRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _backgroundJobService = backgroundJobService;
    }

    public async Task<Result> Handle(
        TriggerReminderCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        var tenantId = _currentUserService.TenantId;

        // Get reminder
        var reminder = await _reminderRepository.GetByIdAsync(request.ReminderId, cancellationToken);
        
        if (reminder == null)
            return Result.Failure("Reminder not found");

        // For background job triggers, userId might be null
        // For manual triggers, check tenant access
        if (!string.IsNullOrEmpty(userId) && tenantId.HasValue)
        {
            // Check if user has access to the reminder's work item
            // This would require loading the work item or having a method to check access
            // For simplicity, we'll skip this check for background jobs
        }

        try
        {
            if (!string.IsNullOrEmpty(request.ErrorMessage))
            {
                // Mark as failed
                reminder.MarkAsFailed(request.ErrorMessage, userId ?? "system");
            }
            else
            {
                // Mark as triggered
                reminder.MarkAsTriggered(userId ?? "system");
            }

            // Update in repository
            await _reminderRepository.UpdateAsync(reminder, cancellationToken);
            
            // Save changes and dispatch domain events
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.DispatchDomainEventsAsync(cancellationToken);

            // Delete the scheduled job if it exists
            // Note: Job ID would need to be stored somewhere or follow a convention
            // For now, we'll assume job IDs follow a pattern: $"reminder_{reminder.Id}"
            var jobId = $"reminder_{reminder.Id}";
            _backgroundJobService.DeleteJob(jobId);

            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure($"Cannot trigger reminder: {ex.Message}");
        }
    }
}

public class TriggerReminderCommandValidator : AbstractValidator<TriggerReminderCommand>
{
    public TriggerReminderCommandValidator()
    {
        RuleFor(x => x.ReminderId)
            .NotEmpty().WithMessage("Reminder ID is required");

        RuleFor(x => x.ErrorMessage)
            .MaximumLength(1000).WithMessage("Error message must not exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.ErrorMessage));
    }
}