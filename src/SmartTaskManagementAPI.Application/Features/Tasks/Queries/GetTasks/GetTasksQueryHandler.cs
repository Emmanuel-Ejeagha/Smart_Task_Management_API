using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartTaskManagementAPI.Application.Common.Exceptions;
using SmartTaskManagementAPI.Application.Common.Interfaces;
using SmartTaskManagementAPI.Application.Common.Models;
using SmartTaskManagementAPI.Application.Features.Tasks.DTOs;
using SmartTaskManagementAPI.Domain.Enums;

namespace SmartTaskManagementAPI.Application.Features.Tasks.Queries.GetTasks;

public class GetTasksQueryHandler : IRequestHandler<GetTasksQuery, PaginatedResult<TaskListDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly ILogger<GetTasksQueryHandler> _logger;

    public GetTasksQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMapper mapper,
        ILogger<GetTasksQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
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

            // Get current user to get tenant ID
            var currentUser = await _unitOfWork.User.GetByIdAsync(currentUserId, cancellationToken);
            if (currentUser == null || currentUser.IsDeleted)
                throw new NotFoundException("User", currentUserId);

            if (!currentUser.IsActive)
                throw new UnauthorizedAccessException("User account is deactivated");

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

            if (request.OverdueOnly == true)
            {
                filteredTasks = filteredTasks.Where(t => t.IsOverdue());
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

            _logger.LogInformation("Retrieved {Count} tasks for user: {UserId}, Tenant: {TenantId}", 
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