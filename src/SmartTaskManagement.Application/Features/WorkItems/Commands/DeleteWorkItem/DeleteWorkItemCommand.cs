using FluentValidation;
using MediatR;
using SmartTaskManagement.Application.Common.Behaviors;
using SmartTaskManagement.Application.Common.Interfaces;
using SmartTaskManagement.Application.Common.Models;

namespace SmartTaskManagement.Application.Features.WorkItems.Commands.DeleteWorkItem;

[Authorize("Admin")] // Only admins can delete work items
[Transactional]
public class DeleteWorkItemCommand : IRequest<Result>
{
    public Guid Id { get; set; }
}

public class DeleteWorkItemCommandHandler : IRequestHandler<DeleteWorkItemCommand, Result>
{
    private readonly IWorkItemRepository _workItemRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteWorkItemCommandHandler(
        IWorkItemRepository workItemRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _workItemRepository = workItemRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        DeleteWorkItemCommand request,
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

        // Soft delete work item
        workItem.MarkAsDeleted(userId);

        // Update in repository
        await _workItemRepository.UpdateAsync(workItem, cancellationToken);
        
        // Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public class DeleteWorkItemCommandValidator : AbstractValidator<DeleteWorkItemCommand>
{
    public DeleteWorkItemCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Work item ID is required");
    }
}