using FluentValidation;
using MediatR;
using SmartTaskManagement.Application.Common.Behaviors;
using SmartTaskManagement.Application.Common.Interfaces;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Domain.Enums;
using SmartTaskManagement.Domain.Services;

namespace SmartTaskManagement.Application.Features.WorkItems.Commands.ChangeWorkItemState;

[Authorize("User")]
[Transactional]
public class ChangeWorkItemStateCommand : IRequest<Result>
{
    public Guid Id { get; set; }
    public WorkItemState NewState { get; set; }
    public int? ActualHours { get; set; }
}

public class ChangeWorkItemStateCommandHandler : IRequestHandler<ChangeWorkItemStateCommand, Result>
{
    private readonly IWorkItemRepository _workItemRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IWorkItemService _workItemService;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeWorkItemStateCommandHandler(
        IWorkItemRepository workItemRepository,
        ICurrentUserService currentUserService,
        IWorkItemService workItemService,
        IUnitOfWork unitOfWork)
    {
        _workItemRepository = workItemRepository;
        _currentUserService = currentUserService;
        _workItemService = workItemService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        ChangeWorkItemStateCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        var tenantId = _currentUserService.TenantId;

        if (string.IsNullOrEmpty(userId) || !tenantId.HasValue)
            return Result.Failure("User not authenticated or tenant not found");

        // Get work item
        var workItem = await _workItemRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (workItem == null)
            return Result.Failure("Work item not found");

        // Check tenant access
        if (workItem.TenantId != tenantId.Value)
            return Result.Failure("Access denied");

        // Check if state transition is allowed
        if (!_workItemService.CanTransitionToState(workItem, request.NewState))
            return Result.Failure($"Cannot transition from {workItem.State} to {request.NewState}");

        // Perform state transition
        switch (request.NewState)
        {
            case WorkItemState.InProgress:
                workItem.Start(userId);
                break;
                
            case WorkItemState.Completed:
                workItem.Complete(userId, request.ActualHours ?? 0);
                break;
                
            case WorkItemState.Archived:
                workItem.Archive(userId);
                break;
                
            case WorkItemState.Draft:
                workItem.Reopen(userId);
                break;
                
            case WorkItemState.OnHold:
                // Handle on hold transition
                var previousState = workItem.State;
                workItem.State = WorkItemState.OnHold;
                workItem.MarkAsUpdated(userId);
                break;
                
            case WorkItemState.Cancelled:
                // Handle cancelled transition
                previousState = workItem.State;
                workItem.State = WorkItemState.Cancelled;
                workItem.MarkAsUpdated(userId);
                break;
                
            default:
                return Result.Failure($"Unsupported state transition to {request.NewState}");
        }

        // Update in repository
        await _workItemRepository.UpdateAsync(workItem, cancellationToken);
        
        // Save changes and dispatch domain events
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _unitOfWork.DispatchDomainEventsAsync(cancellationToken);

        return Result.Success();
    }
}

public class ChangeWorkItemStateCommandValidator : AbstractValidator<ChangeWorkItemStateCommand>
{
    public ChangeWorkItemStateCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Work item ID is required");

        RuleFor(x => x.NewState)
            .IsInEnum().WithMessage("Invalid work item state");

        RuleFor(x => x.ActualHours)
            .GreaterThanOrEqualTo(0).WithMessage("Actual hours must be 0 or greater")
            .LessThanOrEqualTo(1000).WithMessage("Actual hours must not exceed 1000")
            .When(x => x.ActualHours.HasValue);
    }
}