using FluentValidation;
using MediatR;
using SmartTaskManagement.Application.Common.Behaviors;
using SmartTaskManagement.Application.Common.Interfaces;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Domain.Entities;
using SmartTaskManagement.Domain.Enums;

namespace SmartTaskManagement.Application.Features.WorkItems.Commands.CreateWorkItem;

[Authorize("User")]
[Transactional]
public class CreateWorkItemCommand : IRequest<Result<Guid>>
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public WorkItemPriority Priority { get; set; } = WorkItemPriority.Medium;
    public DateTime? DueDateUtc { get; set; }
    public int EstimatedHours { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class CreateWorkItemCommandHandler : IRequestHandler<CreateWorkItemCommand, Result<Guid>>
{
    private readonly IWorkItemRepository _workItemRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateWorkItemCommandHandler(
        IWorkItemRepository workItemRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _workItemRepository = workItemRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(
        CreateWorkItemCommand request,
        CancellationToken cancellationToken)
    {
        // Get current user and tenant
        var userId = _currentUserService.UserId;
        var tenantId = _currentUserService.TenantId;

        if (string.IsNullOrEmpty(userId) || !tenantId.HasValue)
            return Result<Guid>.Failure("User not authenticated or tenant not found");

        // Check if title is unique within tenant
        var isTitleUnique = await _workItemRepository.IsTitleUniqueAsync(
            tenantId.Value,
            request.Title,
            null,
            cancellationToken);

        if (!isTitleUnique)
            return Result<Guid>.Failure($"Work item with title '{request.Title}' already exists in this tenant");

        // Create work item
        var workItem = new WorkItem(
            tenantId.Value,
            request.Title,
            request.Description,
            request.Priority,
            request.DueDateUtc,
            userId);

        workItem.EstimatedHours = request.EstimatedHours;

        foreach (var tag in request.Tags)
        {
            workItem.AddTag(tag, userId);
        }

        // Add to repository
        await _workItemRepository.AddAsync(workItem, cancellationToken);
        
        // Save changes and dispatch domain events
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _unitOfWork.DispatchDomainEventsAsync(cancellationToken);

        return Result<Guid>.Success(workItem.Id);
    }
}

public class CreateWorkItemCommandValidator : AbstractValidator<CreateWorkItemCommand>
{
    public CreateWorkItemCommandValidator()
    {
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