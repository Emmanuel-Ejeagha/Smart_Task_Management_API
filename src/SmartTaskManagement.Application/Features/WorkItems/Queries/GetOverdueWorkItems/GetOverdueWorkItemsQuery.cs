using AutoMapper;
using MediatR;
using SmartTaskManagement.Application.Common.Behaviors;
using SmartTaskManagement.Application.Common.Interfaces;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Application.Features.WorkItems.Dtos;

namespace SmartTaskManagement.Application.Features.WorkItems.Queries.GetOverdueWorkItems;

[Authorize("User")]
public class GetOverdueWorkItemsQuery : IRequest<Result<IReadOnlyList<WorkItemDto>>>
{
    // Optional: Filter by priority
    public Domain.Enums.WorkItemPriority? Priority { get; set; }
}

public class GetOverdueWorkItemsQueryHandler : IRequestHandler<GetOverdueWorkItemsQuery, Result<IReadOnlyList<WorkItemDto>>>
{
    private readonly IWorkItemRepository _workItemRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly Domain.Services.IWorkItemService _workItemService;

    public GetOverdueWorkItemsQueryHandler(
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

    public async Task<Result<IReadOnlyList<WorkItemDto>>> Handle(
        GetOverdueWorkItemsQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId;

        if (!tenantId.HasValue)
            return Result<IReadOnlyList<WorkItemDto>>.Failure("User not authenticated or tenant not found");

        // Get overdue work items
        var overdueWorkItems = await _workItemRepository.GetOverdueAsync(tenantId.Value, cancellationToken);

        // Filter by priority if specified
        if (request.Priority.HasValue)
        {
            overdueWorkItems = overdueWorkItems
                .Where(w => w.Priority == request.Priority.Value)
                .ToList();
        }

        // Map to DTOs with additional properties
        var dtos = overdueWorkItems.Select(workItem =>
        {
            var dto = _mapper.Map<WorkItemDto>(workItem);
            dto.IsOverdue = true; // They're overdue by definition
            dto.ProgressPercentage = _workItemService.CalculateProgressPercentage(workItem);
            return dto;
        }).ToList();

        return Result<IReadOnlyList<WorkItemDto>>.Success(dtos);
    }
}