using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTaskManagementAPI.API.Controllers.Base;
using SmartTaskManagementAPI.API.Models;
using SmartTaskManagementAPI.Application.Common.Models;
using SmartTaskManagementAPI.Application.Features.Tasks.Commands.ArchiveTask;
using SmartTaskManagementAPI.Application.Features.Tasks.Commands.ChangeTaskStatus;
using SmartTaskManagementAPI.Application.Features.Tasks.Commands.CreateTask;
using SmartTaskManagementAPI.Application.Features.Tasks.Commands.DeleteTask;
using SmartTaskManagementAPI.Application.Features.Tasks.Commands.UpdateTask;
using SmartTaskManagementAPI.Application.Features.Tasks.DTOs;
using SmartTaskManagementAPI.Application.Features.Tasks.Queries.GetTaskById;
using SmartTaskManagementAPI.Application.Features.Tasks.Queries.GetTasks;
using SmartTaskManagementAPI.Domain.Enums;

namespace SmartTaskManagementAPI.API.Controllers.v1;

[ApiController]
[Authorize]
public class TasksController : ApiControllerBase
{
    public TasksController()
    {
    }

    /// <summary>
    /// Get paginated list of tasks with filtering and sorting
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
            var result = await Mediator.Send(query);
            return HandlePaginatedResult(result, "Tasks retrieved successfully");
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.LogWarning(ex, "Unauthorized access to tasks for user: {UserId}", CurrentUserId);
            return Unauthorized(ApiResponse.ErrorResponse("Unauthorized access"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving tasks for user: {UserId}", CurrentUserId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.ErrorResponse("An error occurred while retrieving tasks"));
        }
    }

    /// <summary>
    /// Get a specific task by ID
    /// </summary>
    /// <param name="id">Task ID</param>
    /// <returns>Task details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<TaskDto>>> GetTask(Guid id)
    {
        try
        {
            var query = new GetTaskByIdQuery { TaskId = id };
            var result = await Mediator.Send(query);
            
            if (result == null)
            {
                return NotFound(ApiResponse<TaskDto>.ErrorResponse($"Task with ID {id} not found"));
            }
            
            return HandleResult(result, "Task retrieved successfully");
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.LogWarning(ex, "Unauthorized access to task {TaskId} by user: {UserId}", id, CurrentUserId);
            return Unauthorized(ApiResponse<TaskDto>.ErrorResponse("Unauthorized access to task"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving task {TaskId} for user: {UserId}", id, CurrentUserId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<TaskDto>.ErrorResponse("An error occurred while retrieving the task"));
        }
    }

    /// <summary>
    /// Create a new task
    /// </summary>
    /// <param name="command">Task creation details</param>
    /// <returns>Created task</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<TaskDto>>> CreateTask(CreateTaskCommand command)
    {
        try
        {
            var result = await Mediator.Send(command);
            return CreatedResult(
                result,
                nameof(GetTask),
                new { id = result.Id, version = "1" },
                "Task created successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating task for user: {UserId}", CurrentUserId);
            return BadRequest(ApiResponse<TaskDto>.ErrorResponse("Failed to create task", new List<ApiError>
            {
                new ApiError("CREATE_TASK_ERROR", ex.Message)
            }));
        }
    }

    /// <summary>
    /// Update an existing task
    /// </summary>
    /// <param name="id">Task ID</param>
    /// <param name="command">Task update details</param>
    /// <returns>Updated task</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<TaskDto>>> UpdateTask(Guid id, UpdateTaskCommand command)
    {
        try
        {
            command.TaskId = id;
            var result = await Mediator.Send(command);
            return HandleResult(result, "Task updated successfully");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("archived"))
        {
            Logger.LogWarning(ex, "Attempted to update archived task {TaskId}", id);
            return BadRequest(ApiResponse<TaskDto>.ErrorResponse("Cannot update an archived task"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating task {TaskId} for user: {UserId}", id, CurrentUserId);
            return BadRequest(ApiResponse<TaskDto>.ErrorResponse("Failed to update task", new List<ApiError>
            {
                new ApiError("UPDATE_TASK_ERROR", ex.Message)
            }));
        }
    }

    /// <summary>
    /// Delete a task (soft delete, admin only)
    /// </summary>
    /// <param name="id">Task ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse>> DeleteTask(Guid id)
    {
        try
        {
            var command = new DeleteTaskCommand { TaskId = id };
            await Mediator.Send(command);
            return HandleCommandResult(Unit.Value, "Task deleted successfully");
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.LogWarning(ex, "Non-admin attempted to delete task {TaskId}", id);
            return StatusCode(StatusCodes.Status403Forbidden,
                ApiResponse.ErrorResponse("Only administrators can delete tasks"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting task {TaskId} for user: {UserId}", id, CurrentUserId);
            return BadRequest(ApiResponse.ErrorResponse("Failed to delete task", new List<ApiError>
            {
                new ApiError("DELETE_TASK_ERROR", ex.Message)
            }));
        }
    }

    /// <summary>
    /// Change task status
    /// </summary>
    /// <param name="id">Task ID</param>
    /// <param name="newStatus">New status value</param>
    /// <returns>Updated task</returns>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<TaskDto>>> ChangeTaskStatus(Guid id, [FromBody] TasksStatus newStatus)
    {
        try
        {
            var command = new ChangeTaskStatusCommand { TaskId = id, NewStatus = newStatus };
            var result = await Mediator.Send(command);
            return HandleResult(result, "Task status updated successfully");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("transition"))
        {
            Logger.LogWarning(ex, "Invalid status transition for task {TaskId}", id);
            return BadRequest(ApiResponse<TaskDto>.ErrorResponse("Invalid status transition"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error changing status for task {TaskId}", id);
            return BadRequest(ApiResponse<TaskDto>.ErrorResponse("Failed to change task status", new List<ApiError>
            {
                new ApiError("CHANGE_STATUS_ERROR", ex.Message)
            }));
        }
    }

    /// <summary>
    /// Archive a task
    /// </summary>
    /// <param name="id">Task ID</param>
    /// <returns>Archived task</returns>
    [HttpPost("{id:guid}/archive")]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<TaskDto>>> ArchiveTask(Guid id)
    {
        try
        {
            var command = new ArchiveTaskCommand { TaskId = id };
            var result = await Mediator.Send(command);
            return HandleResult(result, "Task archived successfully");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("archived"))
        {
            Logger.LogWarning(ex, "Attempted to archive already archived task {TaskId}", id);
            return BadRequest(ApiResponse<TaskDto>.ErrorResponse("Task is already archived"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error archiving task {TaskId}", id);
            return BadRequest(ApiResponse<TaskDto>.ErrorResponse("Failed to archive task", new List<ApiError>
            {
                new ApiError("ARCHIVE_TASK_ERROR", ex.Message)
            }));
        }
    }

    /// <summary>
    /// Get tasks by status
    /// </summary>
    /// <param name="status">Task status to filter by</param>
    /// <param name="query">Pagination parameters</param>
    /// <returns>Paginated list of tasks with specific status</returns>
    [HttpGet("status/{status}")]
    [ProducesResponseType(typeof(PaginatedApiResponse<TaskListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginatedApiResponse<TaskListDto>>> GetTasksByStatus(
        TasksStatus status,
        [FromQuery] GetTasksQuery query)
    {
        try
        {
            query.Status = status;
            var result = await Mediator.Send(query);
            return HandlePaginatedResult(result, $"Tasks with status '{status}' retrieved successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving tasks by status {Status} for user: {UserId}", status, CurrentUserId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.ErrorResponse("An error occurred while retrieving tasks by status"));
        }
    }
}