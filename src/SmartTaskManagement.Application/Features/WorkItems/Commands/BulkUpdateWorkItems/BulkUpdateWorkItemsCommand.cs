using FluentValidation;
using MediatR;
using SmartTaskManagement.Application.Common.Behaviors;
using SmartTaskManagement.Application.Common.Interfaces;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Domain.Enums;

namespace SmartTaskManagement.Application.Features.WorkItems.Commands.BulkUpdateWorkItems;

[Authorize("Admin")] // Bulk operations typically require admin rights
[Transactional]
public class BulkUpdateWorkItemsCommand : IRequest<Result<BulkUpdateResult>>
{
    public List<Guid> WorkItemIds { get; set; } = new();
    public WorkItemState? NewState { get; set; }
    public WorkItemPriority? NewPriority { get; set; }
    public DateTime? NewDueDateUtc { get; set; }
    public List<string>? TagsToAdd { get; set; }
    public List<string>? TagsToRemove { get; set; }
}

public class BulkUpdateResult
{
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<BulkUpdateError> Errors { get; set; } = new();
}

public class BulkUpdateError
{
    public Guid WorkItemId { get; set; }
    public string Error { get; set; } = string.Empty;
}

public class BulkUpdateWorkItemsCommandHandler : IRequestHandler<BulkUpdateWorkItemsCommand, Result<BulkUpdateResult>>
{
    private readonly IWorkItemRepository _workItemRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly Domain.Services.IWorkItemService _workItemService;

    public BulkUpdateWorkItemsCommandHandler(
        IWorkItemRepository workItemRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        Domain.Services.IWorkItemService workItemService)
    {
        _workItemRepository = workItemRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _workItemService = workItemService;
    }

    public async Task<Result<BulkUpdateResult>> Handle(
        BulkUpdateWorkItemsCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        var tenantId = _currentUserService.TenantId;

        if (string.IsNullOrEmpty(userId) || !tenantId.HasValue)
            return Result<BulkUpdateResult>.Failure("User not authenticated or tenant not found");

        var result = new BulkUpdateResult();
        var errors = new List<BulkUpdateError>();

        foreach (var workItemId in request.WorkItemIds)
        {
            try
            {
                var workItem = await _workItemRepository.GetByIdAsync(workItemId, cancellationToken);
                
                if (workItem == null)
                {
                    errors.Add(new BulkUpdateError { WorkItemId = workItemId, Error = "Work item not found" });
                    continue;
                }

                // Check tenant access
                if (workItem.TenantId != tenantId.Value)
                {
                    errors.Add(new BulkUpdateError { WorkItemId = workItemId, Error = "Access denied" });
                    continue;
                }

                // Apply updates
                if (request.NewState.HasValue)
                {
                    if (!_workItemService.CanTransitionToState(workItem, request.NewState.Value))
                    {
                        errors.Add(new BulkUpdateError { 
                            WorkItemId = workItemId, 
                            Error = $"Cannot transition from {workItem.State} to {request.NewState.Value}" 
                        });
                        continue;
                    }

                    // Apply state transition
                    // Simplified - in real implementation, you'd call appropriate methods
                    workItem.State = request.NewState.Value;
                    workItem.MarkAsUpdated(userId);
                }

                if (request.NewPriority.HasValue)
                {
                    workItem.Priority = request.NewPriority.Value;
                    workItem.MarkAsUpdated(userId);
                }

                if (request.NewDueDateUtc.HasValue)
                {
                    workItem.DueDateUtc = request.NewDueDateUtc.Value;
                    workItem.MarkAsUpdated(userId);
                }

                if (request.TagsToAdd != null)
                {
                    foreach (var tag in request.TagsToAdd)
                    {
                        workItem.AddTag(tag, userId);
                    }
                }

                if (request.TagsToRemove != null)
                {
                    foreach (var tag in request.TagsToRemove)
                    {
                        workItem.RemoveTag(tag, userId);
                    }
                }

                // Update in repository
                await _workItemRepository.UpdateAsync(workItem, cancellationToken);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                errors.Add(new BulkUpdateError { WorkItemId = workItemId, Error = ex.Message });
            }
        }

        // Save all changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        result.FailureCount = errors.Count;
        result.Errors = errors;

        return Result<BulkUpdateResult>.Success(result);
    }
}

public class BulkUpdateWorkItemsCommandValidator : AbstractValidator<BulkUpdateWorkItemsCommand>
{
    public BulkUpdateWorkItemsCommandValidator()
    {
        RuleFor(x => x.WorkItemIds)
            .NotEmpty().WithMessage("At least one work item ID is required")
            .Must(ids => ids.Count <= 100).WithMessage("Cannot bulk update more than 100 items at once");

        RuleFor(x => x.NewDueDateUtc)
            .GreaterThanOrEqualTo(DateTime.UtcNow.AddMinutes(-5))
            .When(x => x.NewDueDateUtc.HasValue)
            .WithMessage("Due date must be in the future");

        RuleForEach(x => x.TagsToAdd)
            .MaximumLength(50).WithMessage("Tag must not exceed 50 characters")
            .When(x => x.TagsToAdd != null);

        RuleForEach(x => x.TagsToRemove)
            .MaximumLength(50).WithMessage("Tag must not exceed 50 characters")
            .When(x => x.TagsToRemove != null);
    }
}