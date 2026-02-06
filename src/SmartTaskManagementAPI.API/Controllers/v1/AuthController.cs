using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SmartTaskManagementAPI.API.Controllers.Base;
using SmartTaskManagementAPI.API.Models;
using SmartTaskManagementAPI.Application.Features.Auth.Commands.Login;
using SmartTaskManagementAPI.Application.Features.Auth.Commands.RefreshToken;
using SmartTaskManagementAPI.Application.Features.Auth.DTOs;


namespace SmartTaskManagementAPI.API.Controllers.v1;

[ApiController]
public class AuthController : ApiControllerBase
{
    public AuthController()
    {
    }

    /// <summary>
    /// Register a new user and create a tenant
    /// </summary>
    /// <param name="command">User registration details</param>
    /// <returns>Authentication tokens and user information</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register(RegisterCommand command)
    {
        try
        {
            var result = await Mediator.Send(command);
            return CreatedResult(
                result,
                nameof(Login),
                nameof(AuthController).Replace("Controller", ""),
                new { },
                "Registration successful. Welcome to Smart Task Management!");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Registration failed for email: {Email}", command.Email);
            return BadRequest(ApiResponse<AuthResponse>.ErrorResponse("Registration failed", new List<ApiError>
            {
                new ApiError("REGISTRATION_ERROR", ex.Message)
            }));
        }
    }

    /// <summary>
    /// Authenticate user and get tokens
    /// </summary>
    /// <param name="command">User credentials</param>
    /// <returns>Authentication tokens</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(LoginCommand command)
    {
        try
        {
            var result = await Mediator.Send(command);
            return HandleResult(result, "Login successful");
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.LogWarning(ex, "Login failed for email: {Email}", command.Email);
            return Unauthorized(ApiResponse<AuthResponse>.ErrorResponse("Invalid credentials"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Login error for email: {Email}", command.Email);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<AuthResponse>.ErrorResponse("An error occurred during login"));
        }
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    /// <param name="command">Access token and refresh token</param>
    /// <returns>New authentication tokens</returns>
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> RefreshToken(RefreshTokenCommand command)
    {
        try
        {
            var result = await Mediator.Send(command);
            return HandleResult(result, "Token refreshed successfully");
        }
        catch (SecurityTokenException ex)
        {
            Logger.LogWarning(ex, "Token refresh failed");
            return Unauthorized(ApiResponse<AuthResponse>.ErrorResponse("Invalid or expired token"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Token refresh error");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<AuthResponse>.ErrorResponse("An error occurred during token refresh"));
        }
    }

    /// <summary>
    /// Revoke refresh token (logout)
    /// </summary>
    /// <returns>Success status</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse>> Logout()
    {
        try
        {
            await Task.Delay(1);
            return HandleCommandResult(Unit.Value, "Logged out successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Logout error for user: {UserId}", CurrentUserId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.ErrorResponse("An error occurred during logout"));
        }
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    /// <returns>Current user details</returns>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CurrentUserInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public ActionResult<ApiResponse<CurrentUserInfo>> GetCurrentUser()
    {
        try
        {
            var userInfo = new CurrentUserInfo
            {
                UserId = CurrentUserId,
                TenantId = CurrentTenantId,
                Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
                FullName = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
                Roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList(),
                IsAdmin = IsAdmin,
                Claims = User.Claims.Select(c => new ClaimInfo { Type = c.Type, Value = c.Value }).ToList()
            };

            return Ok(ApiResponse<CurrentUserInfo>.SuccessResponse(userInfo, "User information retrieved successfully"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting current user info");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<CurrentUserInfo>.ErrorResponse("An error occurred while retrieving user information"));
        }

    }
}