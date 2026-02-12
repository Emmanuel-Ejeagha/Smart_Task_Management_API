using AutoMapper;
using FluentValidation;
using MediatR;
using SmartTaskManagement.Application.Common.Behaviors;
using SmartTaskManagement.Application.Common.Interfaces;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Application.Features.WorkItems.Dtos;

namespace SmartTaskManagement.Application.Features.WorkItems.Queries.ListWorkItems;

[Authorize("User")]
public class ListWorkItemsQuery : IRequest<Result<PaginatedResult<WorkItemDto>>>
{
    public PaginationRequest Pagination { get; set; } = PaginationRequest.Default;
    public SortingRequest? Sorting { get; set; }
    public FilteringRequest? Filtering { get; set; }
}

public class ListWorkItemsQueryHandler : IRequestHandler<ListWorkItemsQuery, Result<PaginatedResult<WorkItemDto>>>
{
    private readonly IWorkItemRepository _workItemRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly Domain.Services.IWorkItemService _workItemService;

    public ListWorkItemsQueryHandler(
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

    public async Task<Result<PaginatedResult<WorkItemDto>>> Handle(
        ListWorkItemsQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId;

        if (!tenantId.HasValue)
            return Result<PaginatedResult<WorkItemDto>>.Failure("User not authenticated or tenant not found");

        // Get paginated work items
        var paginatedWorkItems = await _workItemRepository.GetByTenantIdAsync(
            tenantId.Value,
            request.Pagination,
            request.Sorting,
            request.Filtering,
            cancellationToken);

        // Map to DTOs with additional properties
        var items = paginatedWorkItems.Items.Select(workItem =>
        {
            var dto = _mapper.Map<WorkItemDto>(workItem);
            dto.IsOverdue = workItem.IsOverdue();
            dto.ProgressPercentage = _workItemService.CalculateProgressPercentage(workItem);
            return dto;
        }).ToList();

        // Create paginated result
        var result = PaginatedResult<WorkItemDto>.Create(
            items,
            paginatedWorkItems.PageNumber,
            paginatedWorkItems.PageSize,
            paginatedWorkItems.TotalCount);

        return Result<PaginatedResult<WorkItemDto>>.Success(result);
    }
}

public class ListWorkItemsQueryValidator : AbstractValidator<ListWorkItemsQuery>
{
    public ListWorkItemsQueryValidator()
    {
        RuleFor(x => x.Pagination)
            .NotNull().WithMessage("Pagination is required");
    }
}