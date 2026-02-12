using AutoMapper;
using FluentValidation;
using MediatR;
using SmartTaskManagement.Application.Common.Behaviors;
using SmartTaskManagement.Application.Common.Interfaces;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Application.Features.WorkItems.Dtos;

namespace SmartTaskManagement.Application.Features.WorkItems.Queries.GetWorkItemById;

[Authorize("User")]
public class GetWorkItemByIdQuery : IRequest<Result<WorkItemDto>>
{
    public Guid Id { get; set; }
}

public class GetWorkItemByIdQueryHandler : IRequestHandler<GetWorkItemByIdQuery, Result<WorkItemDto>>
{
    private readonly IWorkItemRepository _workItemRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly Domain.Services.IWorkItemService _workItemService;

    public GetWorkItemByIdQueryHandler(
        IWorkItemRepository workItemRepository,
        ICurrentUserService currentUserService,
        IMapper mapper,
        Domain.Services.IWorkItemService workItemService)
    {
        _workItemRepository = workItemRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _workItemService = workItemService;
    }

    public async Task<Result<WorkItemDto>> Handle(
        GetWorkItemByIdQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId;

        if (!tenantId.HasValue)
            return Result<WorkItemDto>.Failure("User not authenticated or tenant not found");

        // Get work item
        var workItem = await _workItemRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (workItem == null)
            return Result<WorkItemDto>.Failure("Work item not found");

        // Check tenant access
        if (workItem.TenantId != tenantId.Value)
            return Result<WorkItemDto>.Failure("Access denied");

        // Map to DTO
        var dto = _mapper.Map<WorkItemDto>(workItem);
        
        // Calculate additional properties
        dto.IsOverdue = workItem.IsOverdue();
        dto.ProgressPercentage = _workItemService.CalculateProgressPercentage(workItem);

        return Result<WorkItemDto>.Success(dto);
    }
}

public class GetWorkItemByIdQueryValidator : AbstractValidator<GetWorkItemByIdQuery>
{
    public GetWorkItemByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Work item ID is required");
    }
}