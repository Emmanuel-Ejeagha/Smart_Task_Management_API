using FluentValidation;
using MediatR;
using SmartTaskManagement.Application.Common.Behaviors;
using SmartTaskManagement.Application.Common.Interfaces;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Domain.Entities;
using SmartTaskManagement.Domain.Services;

namespace SmartTaskManagement.Application.Features.WorkItems.Commands.AddReminderToWorkItem;

[Authorize("User")]
[Transactional]
public class AddReminderToWorkItemCommand : IRequest<Result<Guid>>
{
    public Guid WorkItemId { get; set; }
    public DateTime ReminderDateUtc { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class AddReminderToWorkItemCommandHandler : IRequestHandler<AddReminderToWorkItemCommand, Result<Guid>>
{
    private readonly IWorkItemRepository _workItemRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IWorkItemService _workItemService;
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly IUnitOfWork _unitOfWork;

    public AddReminderToWorkItemCommandHandler(
        IWorkItemRepository workItemRepository,
        ICurrentUserService currentUserService,
        IWorkItemService workItemService,
        IBackgroundJobService backgroundJobService,
        IUnitOfWork unitOfWork)
    {
        _workItemRepository = workItemRepository;
        _currentUserService = currentUserService;
        _workItemService = workItemService;
        _backgroundJobService = backgroundJobService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(
        AddReminderToWorkItemCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        var tenantId = _currentUserService.TenantId;

        if (string.IsNullOrEmpty(userId) || !tenantId.HasValue)
            return Result<Guid>.Failure("User not authenticated or tenant not found");

        // Get work item
        var workItem = await _workItemRepository.GetByIdAsync(request.WorkItemId, cancellationToken);
        
        if (workItem == null)
            return Result<Guid>.Failure("Work item not found");

        // Check tenant access
        if (workItem.TenantId != tenantId.Value)
            return Result<Guid>.Failure("Access denied");

        // Check if reminder can be scheduled
        if (!_workItemService.CanScheduleReminder(workItem, request.ReminderDateUtc))
            return Result<Guid>.Failure("Cannot schedule reminder for this work item");

        // Create reminder
        var reminder = new Reminder(
            request.WorkItemId,
            request.ReminderDateUtc,
            request.Message,
            userId);

        // Add reminder to work item
        workItem.AddReminder(reminder, userId);

        // Update in repository
        await _workItemRepository.UpdateAsync(workItem, cancellationToken);
        
        // Save changes and dispatch domain events
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _unitOfWork.DispatchDomainEventsAsync(cancellationToken);

        // Schedule background job for reminder
        var jobId = _backgroundJobService.ScheduleReminder(reminder.Id, reminder.ReminderDateUtc);

        return Result<Guid>.Success(reminder.Id);
    }
}

public class AddReminderToWorkItemCommandValidator : AbstractValidator<AddReminderToWorkItemCommand>
{
    public AddReminderToWorkItemCommandValidator()
    {
        RuleFor(x => x.WorkItemId)
            .NotEmpty().WithMessage("Work item ID is required");

        RuleFor(x => x.ReminderDateUtc)
            .GreaterThan(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("Reminder date must be at least 5 minutes in the future");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required")
            .MaximumLength(500).WithMessage("Message must not exceed 500 characters");
    }
}