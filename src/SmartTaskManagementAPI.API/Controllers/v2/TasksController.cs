using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTaskManagementAPI.API.Models;
using SmartTaskManagementAPI.Application.Features.Tasks.DTOs;
using SmartTaskManagementAPI.Application.Features.Tasks.Queries.GetTaskById;
using SmartTaskManagementAPI.Application.Features.Tasks.Queries.GetTasks;
using SmartTaskManagementAPI.Application.Features.TasksV2;
using SmartTaskManagementAPI.Domain.Enums;

namespace SmartTaskManagementAPI.API.Controllers.v2;

[ApiController]
[Authorize]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
[Consumes("application/json")]
public class TasksController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TasksController> _logger;

    public TasksController(IMediator mediator, ILogger<TasksController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated list of tasks with advanced filtering (v2)
    /// </summary>
    /// <param name="query">Pagination and filtering parameters</param>
    /// <returns>Paginated list of tasks</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedApiResponse<TaskListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginatedApiResponse<TaskListDto>>> GetTasks([FromQuery] GetTasksQuery query)
    {
        try
        {
            var result = await _mediator.Send(query);
            return Ok(PaginatedApiResponse<TaskListDto>.SuccessResponse(
                result.Items,
                result.PageNumber,
                result.PageSize,
                result.TotalCount,
                "Tasks retrieved successfully (v2)"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tasks (v2)");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.ErrorResponse("An error occurred while retrieving tasks"));
        }
    }

    /// <summary>
    /// Get a specific task by ID with additional details (v2)
    /// </summary>
    /// <param name="id">Task ID</param>
    /// <returns>Task details with extended information</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TaskDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<TaskDetailDto>>> GetTask(Guid id)
    {
        try
        {
            var query = new GetTaskByIdQuery { TaskId = id };
            var result = await _mediator.Send(query);
            
            if (result == null)
            {
                return NotFound(ApiResponse<TaskDetailDto>.ErrorResponse($"Task with ID {id} not found"));
            }
            
            // Convert to v2 DTO with additional details
            var v2Result = new TaskDetailDto
            {
                Id = result.Id,
                Title = result.Title,
                Description = result.Description,
                Priority = result.Priority,
                PriorityDisplay = result.PriorityDisplay,
                Status = result.Status,
                StatusDisplay = result.StatusDisplay,
                DueDate = result.DueDate,
                ReminderDate = result.ReminderDate,
                CreatedAt = result.CreatedAt,
                CreatedBy = result.CreatedBy,
                UpdatedAt = result.UpdatedAt,
                UpdatedBy = result.UpdatedBy,
                IsOverdue = result.DueDate.HasValue && result.DueDate < DateTime.UtcNow && result.Status != TasksStatus.Done,
                DaysUntilDue = result.DueDate.HasValue 
                    ? (int)(result.DueDate.Value - DateTime.UtcNow).TotalDays 
                    : null,
                CanBeArchived = result.Status == TasksStatus.Done,
                CanTransitionToInProgress = result.Status == TasksStatus.Draft,
                CanTransitionToDone = result.Status == TasksStatus.InProgress
            };
            
            return Ok(ApiResponse<TaskDetailDto>.SuccessResponse(v2Result, "Task retrieved successfully (v2)"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving task {TaskId} (v2)", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<TaskDetailDto>.ErrorResponse("An error occurred while retrieving the task"));
        }
    }

    /// <summary>
    /// Create a new task with enhanced validation (v2)
    /// </summary>
    /// <param name="command">Task creation details</param>
    /// <returns>Created task</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TaskDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<TaskDetailDto>>> CreateTask(CreateTaskV2Command command)
    {
        try
        {
            var result = await _mediator.Send(command);
            
            // Convert to v2 DTO
            var v2Result = new TaskDetailDto
            {
                Id = result.Id,
                Title = result.Title,
                Description = result.Description,
                Priority = result.Priority,
                PriorityDisplay = result.PriorityDisplay,
                Status = result.Status,
                StatusDisplay = result.StatusDisplay,
                DueDate = result.DueDate,
                ReminderDate = result.ReminderDate,
                CreatedAt = result.CreatedAt,
                CreatedBy = result.CreatedBy,
                UpdatedAt = result.UpdatedAt,
                UpdatedBy = result.UpdatedBy,
                IsOverdue = false,
                DaysUntilDue = result.DueDate.HasValue 
                    ? (int)(result.DueDate.Value - DateTime.UtcNow).TotalDays 
                    : null,
                CanBeArchived = false,
                CanTransitionToInProgress = true,
                CanTransitionToDone = false
            };
            
            return CreatedAtAction(nameof(GetTask), new { id = result.Id, version = "2" }, 
                ApiResponse<TaskDetailDto>.SuccessResponse(v2Result, "Task created successfully (v2)"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating task (v2)");
            return BadRequest(ApiResponse<TaskDetailDto>.ErrorResponse("Failed to create task", new List<ApiError>
            {
                new ApiError("CREATE_TASK_ERROR", ex.Message)
            }));
        }
    }

    /// <summary>
    /// Bulk update task statuses (v2 exclusive feature)
    /// </summary>
    /// <param name="command">Bulk status update details</param>
    /// <returns>Success status</returns>
    [HttpPost("bulk/status")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse>> BulkUpdateStatus(BulkUpdateTaskStatusCommand command)
    {
        try
        {
            await _mediator.Send(command);
            return Ok(ApiResponse.SuccessResponse("Task statuses updated successfully (v2)"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk status update (v2)");
            return BadRequest(ApiResponse.ErrorResponse("Failed to update task statuses", new List<ApiError>
            {
                new ApiError("BULK_UPDATE_ERROR", ex.Message)
            }));
        }
    }

    /// <summary>
    /// Get task statistics for current user (v2 exclusive feature)
    /// </summary>
    /// <returns>Task statistics</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ApiResponse<TaskStatisticsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<TaskStatisticsDto>>> GetStatistics()
    {
        try
        {
            var query = new GetTaskStatisticsQuery();
            var result = await _mediator.Send(query);
            return Ok(ApiResponse<TaskStatisticsDto>.SuccessResponse(result, "Task statistics retrieved (v2)"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving task statistics (v2)");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<TaskStatisticsDto>.ErrorResponse("An error occurred while retrieving statistics"));
        }
    }
}
