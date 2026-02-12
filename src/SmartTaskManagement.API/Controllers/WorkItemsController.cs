using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTaskManagement.API.Controllers.Base;
using SmartTaskManagement.API.Models;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Application.Features.Reminders.Dtos;
using SmartTaskManagement.Application.Features.WorkItems.Commands.AddReminderToWorkItem;
using SmartTaskManagement.Application.Features.WorkItems.Commands.ChangeWorkItemState;
using SmartTaskManagement.Application.Features.WorkItems.Commands.CreateWorkItem;
using SmartTaskManagement.Application.Features.WorkItems.Commands.DeleteWorkItem;
using SmartTaskManagement.Application.Features.WorkItems.Commands.UpdateWorkItem;
using SmartTaskManagement.Application.Features.WorkItems.Dtos;
using SmartTaskManagement.Application.Features.WorkItems.Queries.GetOverdueWorkItems;
using SmartTaskManagement.Application.Features.WorkItems.Queries.GetWorkItemById;
using SmartTaskManagement.Application.Features.WorkItems.Queries.GetWorkItemReminders;
using SmartTaskManagement.Application.Features.WorkItems.Queries.ListWorkItems;
using SmartTaskManagement.Domain.Enums;
using Swashbuckle.AspNetCore.Annotations;

namespace SmartTaskManagement.API.Controllers;

/// <summary>
/// Manages work items (tasks)
/// </summary>
[ApiVersion("1.0")]
public class WorkItemsController : ApiControllerBase
{
    /// <summary>
    /// Get a work item by ID
    /// </summary>
    /// <param name="id">Work item ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Work item details</returns>
    [SwaggerOperation(Summary = "Retrieves a specific work item by its unique identifier")]
    [SwaggerResponse(200, "Work item found", typeof(ApiResponse<WorkItemDto>))]
    [SwaggerResponse(404, "Work item not found", typeof(ProblemDetails))]
    [SwaggerResponse(401, "Unauthorized - valid JWT token required")]
        [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<WorkItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetWorkItemByIdQuery { Id = id };
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// List work items with pagination, sorting, and filtering
    /// </summary>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="sorting">Sorting parameters (optional)</param>
    /// <param name="filtering">Filtering parameters (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of work items</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<WorkItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> List(
        [FromQuery] PaginationRequest pagination,
        [FromQuery] SortingRequest? sorting = null,
        [FromQuery] FilteringRequest? filtering = null,
        CancellationToken cancellationToken = default)
    {
        var query = new ListWorkItemsQuery
        {
            Pagination = pagination,
            Sorting = sorting,
            Filtering = filtering
        };
        var result = await Mediator.Send(query, cancellationToken);
        return HandlePaginatedResult(result);
    }

    /// <summary>
    /// Create a new work item
    /// </summary>
    /// <param name="command">Work item creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created work item ID</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(
        [FromBody] CreateWorkItemCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        if (result.IsSuccess)
        {
            var uri = Url.Action(nameof(GetById), new { id = result.Value }) 
                ?? $"/api/v1/workitems/{result.Value}";
            return Created(uri, result.Value, "Work item created successfully");
        }
        return HandleResult(result);
    }

    /// <summary>
    /// Update an existing work item
    /// </summary>
    /// <param name="id">Work item ID</param>
    /// <param name="command">Work item update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateWorkItemCommand command,
        CancellationToken cancellationToken)
    {
        command.Id = id;
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Delete a work item (soft delete)
    /// </summary>
    /// <remarks>Only users with Admin role can delete work items</remarks>
    /// <param name="id">Work item ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteWorkItemCommand { Id = id };
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Change work item state (e.g., start, complete, archive)
    /// </summary>
    /// <param name="id">Work item ID</param>
    /// <param name="command">State change data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpPatch("{id:guid}/state")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangeState(
        Guid id,
        [FromBody] ChangeWorkItemStateCommand command,
        CancellationToken cancellationToken)
    {
        command.Id = id;
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get reminders for a work item
    /// </summary>
    /// <param name="id">Work item ID</param>
    /// <param name="status">Optional filter by reminder status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of reminders</returns>
    [HttpGet("{id:guid}/reminders")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ReminderDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetReminders(
        Guid id,
        [FromQuery] ReminderStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetWorkItemRemindersQuery 
        { 
            WorkItemId = id,
            Status = status 
        };
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Add a reminder to a work item
    /// </summary>
    /// <param name="id">Work item ID</param>
    /// <param name="command">Reminder data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created reminder ID</returns>
    [HttpPost("{id:guid}/reminders")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddReminder(
        Guid id,
        [FromBody] AddReminderToWorkItemCommand command,
        CancellationToken cancellationToken)
    {
        command.WorkItemId = id;
        var result = await Mediator.Send(command, cancellationToken);
        if (result.IsSuccess)
        {
            var uri = Url.Action(nameof(GetReminders), new { id }) 
                ?? $"/api/v1/workitems/{id}/reminders";
            return Created(uri, result.Value, "Reminder added successfully");
        }
        return HandleResult(result);
    }

    /// <summary>
    /// Get overdue work items for the current tenant
    /// </summary>
    /// <param name="priority">Optional priority filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of overdue work items</returns>
    [HttpGet("overdue")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<WorkItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetOverdue(
        [FromQuery] WorkItemPriority? priority = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetOverdueWorkItemsQuery { Priority = priority };
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }
}