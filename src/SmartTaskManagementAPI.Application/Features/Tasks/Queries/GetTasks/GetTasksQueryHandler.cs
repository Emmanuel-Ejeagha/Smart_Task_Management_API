using System;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartTaskManagementAPI.Application.Common.Exceptions;
using SmartTaskManagementAPI.Application.Common.Interfaces;
using SmartTaskManagementAPI.Application.Common.Models;
using SmartTaskManagementAPI.Application.Features.Tasks.DTOs;
using SmartTaskManagementAPI.Application.Interfaces;

namespace SmartTaskManagementAPI.Application.Features.Tasks.Queries.GetTasks;

public class GetTasksQueryHandler : IRequestHandler<GetTasksQuery, PaginatedResult<TaskListDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantAccessChecker _tenantAccessChecker;
    private readonly IMapper _mapper;
    private readonly ILogger<GetTasksQueryHandler> _logger;

    public GetTasksQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ITenantAccessChecker tenantAccessChecker,
        IMapper mapper,
        ILogger<GetTasksQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _tenantAccessChecker = tenantAccessChecker;
        _mapper = mapper;
        _logger = logger;        
    }

    public async Task<PaginatedResult<TaskListDto>> Handle(GetTasksQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Get current user info
            if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
                throw new UnauthorizedAccessException("User not authenticated");

            // Get current user to get tenant isolation
            var currentUser = await _unitOfWork.User.GetByIdAsync(currentUserId, cancellationToken);
            if (currentUser == null || currentUser.IsDeleted)
                throw new NotFoundException("User", currentUserId);

            // Get paginated tasks with tenant isolation
            var paginatedTasks = await _unitOfWork.Tasks.GetPaginatedAsync(
                currentUser.TenantId,
                request.Pagination,
                cancellationToken);

            // Apply additional filters
            var filteredTasks = paginatedTasks.Items.AsQueryable();

            if (request.Status.HasValue)
            {
                filteredTasks = filteredTasks.Where(t => t.Status == request.Status.Value);
            }

            if (request.Priority.HasValue)
            {
                filteredTasks = filteredTasks.Where(t => t.Priority == request.Priority.Value);
            }

            if (request.OverDueOnly == true)
            {
                filteredTasks = filteredTasks.Where(t => t.IsOverDue());
            }

            // Apply the filters to the paginated result
            var filteredList = filteredTasks.ToList();
            var totalCount = filteredList.Count;

            // Apply pagination to filtered results
            var pagedTasks = filteredList
                .Skip((request.Pagination.PageNumber - 1) * request.Pagination.PageSize)
                .Take(request.Pagination.PageSize)
                .ToList();

            // Map to DTO
            var taskDtos = _mapper.Map<List<TaskListDto>>(pagedTasks);

            _logger.LogInformation("Retrived {Count} tasks for user: {UserId}, Tenant: {TenantId}",
                taskDtos.Count, currentUserId, currentUser.TenantId);

            return new PaginatedResult<TaskListDto>(
                taskDtos,
                totalCount,
                request.Pagination.PageNumber,
                request.Pagination.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tasks for user: {UserId}", _currentUserService.UserId);
            throw;
        }
    }
}
