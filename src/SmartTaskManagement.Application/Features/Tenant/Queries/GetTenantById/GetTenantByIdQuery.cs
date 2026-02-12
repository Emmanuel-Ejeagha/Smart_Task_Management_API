using AutoMapper;
using FluentValidation;
using MediatR;
using SmartTaskManagement.Application.Common.Behaviors;
using SmartTaskManagement.Application.Common.Interfaces;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Application.Features.Tenants.Dtos;

namespace SmartTaskManagement.Application.Features.Tenants.Queries.GetTenantById;

[Authorize]
public class GetTenantByIdQuery : IRequest<Result<TenantDto>>
{
    public Guid Id { get; set; }
}

public class GetTenantByIdQueryHandler : IRequestHandler<GetTenantByIdQuery, Result<TenantDto>>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IWorkItemRepository _workItemRepository;
    private readonly IMapper _mapper;

    public GetTenantByIdQueryHandler(
        ITenantRepository tenantRepository,
        ICurrentUserService currentUserService,
        IWorkItemRepository workItemRepository,
        IMapper mapper)
    {
        _tenantRepository = tenantRepository;
        _currentUserService = currentUserService;
        _workItemRepository = workItemRepository;
        _mapper = mapper;
    }

    public async Task<Result<TenantDto>> Handle(
        GetTenantByIdQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        var tenantId = _currentUserService.TenantId;

        if (string.IsNullOrEmpty(userId))
            return Result<TenantDto>.Failure("User not authenticated");

        // Get tenant
        var tenant = await _tenantRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (tenant == null)
            return Result<TenantDto>.Failure("Tenant not found");

        // Check access: user must be admin or belong to this tenant
        if (!_currentUserService.IsAdmin && tenantId != tenant.Id)
            return Result<TenantDto>.Failure("Access denied");

        // Map to DTO
        var dto = _mapper.Map<TenantDto>(tenant);

        // Get work item count for this tenant
        // Note: This could be optimized with a count method in repository
        var workItems = await _workItemRepository.GetByTenantIdAsync(
            tenant.Id,
            new PaginationRequest { PageNumber = 1, PageSize = 1 },
            cancellationToken: cancellationToken);
        
        dto.WorkItemCount = workItems.TotalCount;

        return Result<TenantDto>.Success(dto);
    }
}

public class GetTenantByIdQueryValidator : AbstractValidator<GetTenantByIdQuery>
{
    public GetTenantByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Tenant ID is required");
    }
}