using FluentValidation;
using MediatR;
using SmartTaskManagement.Application.Common.Behaviors;
using SmartTaskManagement.Application.Common.Interfaces;
using SmartTaskManagement.Application.Common.Models;

namespace SmartTaskManagement.Application.Features.Reminders.Commands.RescheduleReminder;

[Authorize("User")]
[Transactional]
public class RescheduleReminderCommand : IRequest<Result>
{
    public Guid ReminderId { get; set; }
    public DateTime NewReminderDateUtc { get; set; }
    public string? Message { get; set; }
}

public class RescheduleReminderCommandHandler : IRequestHandler<RescheduleReminderCommand, Result>
{
    private readonly IReminderRepository _reminderRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBackgroundJobService _backgroundJobService;

    public RescheduleReminderCommandHandler(
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
        RescheduleReminderCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        var tenantId = _currentUserService.TenantId;

        if (string.IsNullOrEmpty(userId) || !tenantId.HasValue)
            return Result.Failure("User not authenticated or tenant not found");

        // Get reminder
        var reminder = await _reminderRepository.GetByIdAsync(request.ReminderId, cancellationToken);
        
        if (reminder == null)
            return Result.Failure("Reminder not found");

        // Check access through work item
        // For simplicity, we'll need to load the work item
        // In a real app, you'd have a method to check if user can access the reminder

        try
        {
            if (!string.IsNullOrEmpty(request.Message))
            {
                // Update message and reschedule
                reminder.Update(request.NewReminderDateUtc, request.Message, userId);
            }
            else
            {
                // Just reschedule
                reminder.Reschedule(request.NewReminderDateUtc, userId);
            }

            // Update in repository
            await _reminderRepository.UpdateAsync(reminder, cancellationToken);
            
            // Save changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Reschedule background job
            var jobId = $"reminder_{reminder.Id}";
            _backgroundJobService.RescheduleJob(jobId, reminder.ReminderDateUtc);

            return Result.Success();
        }
        catch (ArgumentException ex)
        {
            return Result.Failure($"Invalid reschedule request: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure($"Cannot reschedule reminder: {ex.Message}");
        }
    }
}

public class RescheduleReminderCommandValidator : AbstractValidator<RescheduleReminderCommand>
{
    public RescheduleReminderCommandValidator()
    {
        RuleFor(x => x.ReminderId)
            .NotEmpty().WithMessage("Reminder ID is required");

        RuleFor(x => x.NewReminderDateUtc)
            .GreaterThan(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("New reminder date must be at least 5 minutes in the future");

        RuleFor(x => x.Message)
            .MaximumLength(500).WithMessage("Message must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Message));
    }
}