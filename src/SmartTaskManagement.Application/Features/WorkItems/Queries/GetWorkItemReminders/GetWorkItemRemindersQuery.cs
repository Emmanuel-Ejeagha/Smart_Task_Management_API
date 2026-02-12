using AutoMapper;
using FluentValidation;
using MediatR;
using SmartTaskManagement.Application.Common.Behaviors;
using SmartTaskManagement.Application.Common.Interfaces;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Application.Features.Reminders.Dtos;
using SmartTaskManagement.Domain.Enums;

namespace SmartTaskManagement.Application.Features.WorkItems.Queries.GetWorkItemReminders;

[Authorize("User")]
public class GetWorkItemRemindersQuery : IRequest<Result<IReadOnlyList<ReminderDto>>>
{
    public Guid WorkItemId { get; set; }
    public ReminderStatus? Status { get; set; }
}

public class GetWorkItemRemindersQueryHandler : IRequestHandler<GetWorkItemRemindersQuery, Result<IReadOnlyList<ReminderDto>>>
{
    private readonly IWorkItemRepository _workItemRepository;
    private readonly IReminderRepository _reminderRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetWorkItemRemindersQueryHandler(
        IWorkItemRepository workItemRepository,
        IReminderRepository reminderRepository,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _workItemRepository = workItemRepository;
        _reminderRepository = reminderRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<ReminderDto>>> Handle(
        GetWorkItemRemindersQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId;

        if (!tenantId.HasValue)
            return Result<IReadOnlyList<ReminderDto>>.Failure("User not authenticated or tenant not found");

        // Get work item to check tenant access
        var workItem = await _workItemRepository.GetByIdAsync(request.WorkItemId, cancellationToken);
        
        if (workItem == null)
            return Result<IReadOnlyList<ReminderDto>>.Failure("Work item not found");

        // Check tenant access
        if (workItem.TenantId != tenantId.Value)
            return Result<IReadOnlyList<ReminderDto>>.Failure("Access denied");

        // Get reminders for work item
        var reminders = await _reminderRepository.GetByWorkItemIdAsync(request.WorkItemId, cancellationToken);

        // Filter by status if specified
        if (request.Status.HasValue)
        {
            reminders = reminders.Where(r => r.Status == request.Status.Value).ToList();
        }

        // Map to DTOs
        var dtos = _mapper.Map<IReadOnlyList<ReminderDto>>(reminders);

        return Result<IReadOnlyList<ReminderDto>>.Success(dtos);
    }
}

public class GetWorkItemRemindersQueryValidator : AbstractValidator<GetWorkItemRemindersQuery>
{
    public GetWorkItemRemindersQueryValidator()
    {
        RuleFor(x => x.WorkItemId)
            .NotEmpty().WithMessage("Work item ID is required");
    }
}