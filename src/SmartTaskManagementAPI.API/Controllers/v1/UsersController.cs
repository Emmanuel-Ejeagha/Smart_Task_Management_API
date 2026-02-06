using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTaskManagementAPI.API.Controllers.Base;
using SmartTaskManagementAPI.API.Models;
using SmartTaskManagementAPI.Application.Features.Users.Commands;
using SmartTaskManagementAPI.Application.Features.Users.DTOs;
using SmartTaskManagementAPI.Application.Features.Users.Queries;


namespace SmartTaskManagementAPI.API.Controllers.v1;

[ApiController]
[Authorize]
public class UsersController : ApiControllerBase
{
    public UsersController()
    {
    }

    /// <summary>
    /// Get all users in current tenant (Admin only)
    /// </summary>
    /// <param name="query">Pagination parameters</param>
    /// <returns>List of users in tenant</returns>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PaginatedApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedApiResponse<UserDto>>> GetAllUsers([FromQuery] GetAllUsersQuery query)
    {
        try
        {
            // Set tenant ID from current user
            query.TenantId = CurrentTenantId;
            var result = await Mediator.Send(query);
            return HandlePaginatedResult(result, "Users retrieved successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving users for tenant: {TenantId}", CurrentTenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.ErrorResponse("An error occurred while retrieving users"));
        }
    }

    /// <summary>
    /// Get a specific user by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(Guid id)
    {
        try
        {
            var query = new GetUserByIdQuery { UserId = id, TenantId = CurrentTenantId };
            var result = await Mediator.Send(query);
            
            if (result == null)
            {
                return NotFound(ApiResponse<UserDto>.ErrorResponse($"User with ID {id} not found"));
            }
            
            return HandleResult(result, "User retrieved successfully");
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.LogWarning(ex, "Unauthorized access to user {UserId} by user: {CurrentUserId}", id, CurrentUserId);
            return Unauthorized(ApiResponse<UserDto>.ErrorResponse("Unauthorized access to user"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving user {UserId} for user: {CurrentUserId}", id, CurrentUserId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<UserDto>.ErrorResponse("An error occurred while retrieving the user"));
        }
    }

    /// <summary>
    /// Get current user profile
    /// </summary>
    /// <returns>Current user details</returns>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser()
    {
        try
        {
            var query = new GetUserByIdQuery { UserId = CurrentUserId, TenantId = CurrentTenantId };
            var result = await Mediator.Send(query);
            
            if (result == null)
            {
                return NotFound(ApiResponse<UserDto>.ErrorResponse($"Current user not found"));
            }
            
            return HandleResult(result, "User profile retrieved successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving current user profile for user: {UserId}", CurrentUserId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<UserDto>.ErrorResponse("An error occurred while retrieving the user profile"));
        }
    }

    /// <summary>
    /// Update current user profile
    /// </summary>
    /// <param name="command">User update details</param>
    /// <returns>Updated user</returns>
    [HttpPut("me")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateCurrentUser(UpdateUserCommand command)
    {
        try
        {
            // Ensure user can only update themselves
            command.UserId = CurrentUserId;
            command.TenantId = CurrentTenantId;
            
            var result = await Mediator.Send(command);
            return HandleResult(result, "User profile updated successfully");
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.LogWarning(ex, "Unauthorized update attempt by user: {UserId}", CurrentUserId);
            return Unauthorized(ApiResponse<UserDto>.ErrorResponse("Unauthorized update attempt"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating user profile for user: {UserId}", CurrentUserId);
            return BadRequest(ApiResponse<UserDto>.ErrorResponse("Failed to update user profile", new List<ApiError>
            {
                new ApiError("UPDATE_USER_ERROR", ex.Message)
            }));
        }
    }

    /// <summary>
    /// Update a user (Admin only)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="command">User update details</param>
    /// <returns>Updated user</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(Guid id, UpdateUserCommand command)
    {
        try
        {
            command.UserId = id;
            command.TenantId = CurrentTenantId;
            
            var result = await Mediator.Send(command);
            return HandleResult(result, "User updated successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating user {UserId} for admin: {AdminId}", id, CurrentUserId);
            return BadRequest(ApiResponse<UserDto>.ErrorResponse("Failed to update user", new List<ApiError>
            {
                new ApiError("UPDATE_USER_ERROR", ex.Message)
            }));
        }
    }

    /// <summary>
    /// Deactivate a user (Admin only)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>Success status</returns>
    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<UserDto>>> DeactivateUser(Guid id)
    {
        try
        {
            var command = new DeactivateUserCommand { UserId = id, TenantId = CurrentTenantId };
            var result = await Mediator.Send(command);
            return HandleResult(result, "User deactivated successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deactivating user {UserId}", id);
            return BadRequest(ApiResponse<UserDto>.ErrorResponse("Failed to deactivate user", new List<ApiError>
            {
                new ApiError("DEACTIVATE_USER_ERROR", ex.Message)
            }));
        }
    }

    /// <summary>
    /// Activate a user (Admin only)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>Success status</returns>
    [HttpPatch("{id:guid}/activate")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<UserDto>>> ActivateUser(Guid id)
    {
        try
        {
            var command = new ActivateUserCommand { UserId = id, TenantId = CurrentTenantId };
            var result = await Mediator.Send(command);
            return HandleResult(result, "User activated successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error activating user {UserId}", id);
            return BadRequest(ApiResponse<UserDto>.ErrorResponse("Failed to activate user", new List<ApiError>
            {
                new ApiError("ACTIVATE_USER_ERROR", ex.Message)
            }));
        }
    }
}

