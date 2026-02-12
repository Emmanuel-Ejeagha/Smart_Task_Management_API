using FluentValidation;
using MediatR;
using SmartTaskManagement.Application.Common.Behaviors;
using SmartTaskManagement.Application.Common.Exceptions;
using SmartTaskManagement.Application.Common.Interfaces;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Domain.Enums;

namespace SmartTaskManagement.Application.Features.WorkItems.Commands.UpdateWorkItem;

[Authorize("User")]
[Transactional]
public class UpdateWorkItemCommand : IRequest<Result>
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public WorkItemPriority Priority { get; set; }
    public DateTime? DueDateUtc { get; set; }
    public int EstimatedHours { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class UpdateWorkItemCommandHandler : IRequestHandler<UpdateWorkItemCommand, Result>
{
    private readonly IWorkItemRepository _workItemRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateWorkItemCommandHandler(
        IWorkItemRepository workItemRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _workItemRepository = workItemRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        UpdateWorkItemCommand request,
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

        // Check if title is unique within tenant
        var isTitleUnique = await _workItemRepository.IsTitleUniqueAsync(
            tenantId.Value,
            request.Title,
            request.Id,
            cancellationToken);

        if (!isTitleUnique)
            return Result.Failure($"Work item with title '{request.Title}' already exists in this tenant");

        // Update work item
        workItem.Update(
            request.Title,
            request.Description,
            request.Priority,
            request.DueDateUtc,
            request.EstimatedHours,
            userId);

        // Update tags
        var currentTags = workItem.Tags.ToList();
        
        // Remove tags not in new list
        foreach (var tag in currentTags.Where(t => !request.Tags.Contains(t, StringComparer.OrdinalIgnoreCase)))
        {
            workItem.RemoveTag(tag, userId);
        }
        
        // Add new tags
        foreach (var tag in request.Tags.Where(t => !currentTags.Contains(t, StringComparer.OrdinalIgnoreCase)))
        {
            workItem.AddTag(tag, userId);
        }

        // Update in repository
        await _workItemRepository.UpdateAsync(workItem, cancellationToken);
        
        // Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public class UpdateWorkItemCommandValidator : AbstractValidator<UpdateWorkItemCommand>
{
    public UpdateWorkItemCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Work item ID is required");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters");

        RuleFor(x => x.EstimatedHours)
            .GreaterThanOrEqualTo(0).WithMessage("Estimated hours must be 0 or greater")
            .LessThanOrEqualTo(1000).WithMessage("Estimated hours must not exceed 1000");

        RuleFor(x => x.DueDateUtc)
            .GreaterThanOrEqualTo(DateTime.UtcNow.AddMinutes(-5))
            .When(x => x.DueDateUtc.HasValue)
            .WithMessage("Due date must be in the future");

        RuleForEach(x => x.Tags)
            .MaximumLength(50).WithMessage("Tag must not exceed 50 characters");

        RuleFor(x => x.Tags)
            .Must(tags => tags.Count <= 10)
            .WithMessage("Cannot have more than 10 tags");
    }
}