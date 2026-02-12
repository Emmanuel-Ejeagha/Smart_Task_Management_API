using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTaskManagement.API.Controllers.Base;
using SmartTaskManagement.API.Models;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Application.Features.Reminders.Commands.RescheduleReminder;
using SmartTaskManagement.Application.Features.Reminders.Commands.TriggerReminder;
using SmartTaskManagement.Application.Features.Reminders.Dtos;
using SmartTaskManagement.Application.Features.Reminders.Queries.GetDueReminders;
using SmartTaskManagement.Domain.Enums;

namespace SmartTaskManagement.API.Controllers;

/// <summary>
/// Manages reminders
/// </summary>
[ApiVersion("1.0")]
public class RemindersController : ApiControllerBase
{
    /// <summary>
    /// Trigger a reminder manually (usually called by background jobs)
    /// </summary>
    /// <remarks>
    /// This endpoint is typically used by Hangfire jobs, but can be invoked manually by admins for testing.
    /// </remarks>
    /// <param name="id">Reminder ID</param>
    /// <param name="command">Trigger data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpPost("{id:guid}/trigger")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Trigger(
        Guid id,
        [FromBody] TriggerReminderCommand command,
        CancellationToken cancellationToken)
    {
        command.ReminderId = id;
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Reschedule a reminder
    /// </summary>
    /// <param name="id">Reminder ID</param>
    /// <param name="command">Reschedule data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpPut("{id:guid}/reschedule")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Reschedule(
        Guid id,
        [FromBody] RescheduleReminderCommand command,
        CancellationToken cancellationToken)
    {
        command.ReminderId = id;
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Cancel a reminder (Admin only)
    /// </summary>
    /// <param name="id">Reminder ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var command = new TriggerReminderCommand 
        { 
            ReminderId = id,
            ErrorMessage = "Cancelled by user"
        };
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get due reminders (Admin only)
    /// </summary>
    /// <remarks>
    /// Returns reminders that are scheduled and due now. Intended for monitoring.
    /// </remarks>
    /// <param name="limit">Maximum number of reminders to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of due reminders</returns>
    [HttpGet("due")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ReminderDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDueReminders(
        [FromQuery] int? limit = 100,
        CancellationToken cancellationToken = default)
    {
        var query = new GetDueRemindersQuery 
        { 
            Limit = limit 
        };
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }
}