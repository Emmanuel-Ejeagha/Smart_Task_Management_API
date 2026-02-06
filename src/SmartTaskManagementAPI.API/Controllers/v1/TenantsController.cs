

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTaskManagementAPI.API.Controllers.Base;
using SmartTaskManagementAPI.API.Models;
using SmartTaskManagementAPI.Application.Features.Tenants.Commands;
using SmartTaskManagementAPI.Application.Features.Tenants.DTOs;
using SmartTaskManagementAPI.Application.Features.Tenants.Queries;

namespace SmartTaskManagementAPI.API.Controllers.v1;

[ApiController]
[Authorize(Roles = "Admin")]
public class TenantsController : ApiControllerBase
{
    public TenantsController()
    {
    }

    /// <summary>
    /// Get all tenants (Admin only)
    /// </summary>
    /// <param name="query">Pagination parameters</param>
    /// <returns>List of all tenants</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedApiResponse<TenantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedApiResponse<TenantDto>>> GetAllTenants([FromQuery] GetAllTenantsQuery query)
    {
        try
        {
            var result = await Mediator.Send(query);
            return HandlePaginatedResult(result, "Tenants retrieved successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving all tenants for user: {UserId}", CurrentUserId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.ErrorResponse("An error occurred while retrieving tenants"));
        }
    }

    /// <summary>
    /// Get a specific tenant by ID (Admin only)
    /// </summary>
    /// <param name="id">Tenant ID</param>
    /// <returns>Tenant details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TenantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<TenantDto>>> GetTenant(Guid id)
    {
        try
        {
            var query = new GetTenantByIdQuery { TenantId = id };
            var result = await Mediator.Send(query);
            
            if (result == null)
            {
                return NotFound(ApiResponse<TenantDto>.ErrorResponse($"Tenant with ID {id} not found"));
            }
            
            return HandleResult(result, "Tenant retrieved successfully");
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.LogWarning(ex, "Unauthorized access to tenant {TenantId} by user: {UserId}", id, CurrentUserId);
            return Unauthorized(ApiResponse<TenantDto>.ErrorResponse("Unauthorized access"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving tenant {TenantId} for user: {UserId}", id, CurrentUserId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<TenantDto>.ErrorResponse("An error occurred while retrieving the tenant"));
        }
    }

    /// <summary>
    /// Create a new tenant (System Admin only)
    /// </summary>
    /// <param name="command">Tenant creation details</param>
    /// <returns>Created tenant</returns>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<TenantDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<TenantDto>>> CreateTenant(CreateTenantCommand command)
    {
        try
        {
            var result = await Mediator.Send(command);
            return CreatedResult(
                result,
                nameof(GetTenant),
                new { id = result.Id, version = "1" },
                "Tenant created successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating tenant for user: {UserId}", CurrentUserId);
            return BadRequest(ApiResponse<TenantDto>.ErrorResponse("Failed to create tenant", new List<ApiError>
            {
                new ApiError("CREATE_TENANT_ERROR", ex.Message)
            }));
        }
    }

    /// <summary>
    /// Update an existing tenant (Admin only)
    /// </summary>
    /// <param name="id">Tenant ID</param>
    /// <param name="command">Tenant update details</param>
    /// <returns>Updated tenant</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TenantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<TenantDto>>> UpdateTenant(Guid id, UpdateTenantCommand command)
    {
        try
        {
            command.TenantId = id;
            var result = await Mediator.Send(command);
            return HandleResult(result, "Tenant updated successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating tenant {TenantId} for user: {UserId}", id, CurrentUserId);
            return BadRequest(ApiResponse<TenantDto>.ErrorResponse("Failed to update tenant", new List<ApiError>
            {
                new ApiError("UPDATE_TENANT_ERROR", ex.Message)
            }));
        }
    }

    /// <summary>
    /// Deactivate a tenant (Admin only)
    /// </summary>
    /// <param name="id">Tenant ID</param>
    /// <returns>Success status</returns>
    [HttpPatch("{id:guid}/deactivate")]
    [ProducesResponseType(typeof(ApiResponse<TenantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<TenantDto>>> DeactivateTenant(Guid id)
    {
        try
        {
            var command = new DeactivateTenantCommand { TenantId = id };
            var result = await Mediator.Send(command);
            return HandleResult(result, "Tenant deactivated successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deactivating tenant {TenantId}", id);
            return BadRequest(ApiResponse<TenantDto>.ErrorResponse("Failed to deactivate tenant", new List<ApiError>
            {
                new ApiError("DEACTIVATE_TENANT_ERROR", ex.Message)
            }));
        }
    }

    /// <summary>
    /// Get current user's tenant
    /// </summary>
    /// <returns>Current tenant details</returns>
    [HttpGet("current")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<TenantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<TenantDto>>> GetCurrentTenant()
    {
        try
        {
            // Get tenant ID from claims
            var tenantIdClaim = User.FindFirstValue("TenantId");
            if (string.IsNullOrEmpty(tenantIdClaim) || !Guid.TryParse(tenantIdClaim, out var tenantId))
            {
                return BadRequest(ApiResponse<TenantDto>.ErrorResponse("Tenant ID not found in claims"));
            }

            var query = new GetTenantByIdQuery { TenantId = tenantId };
            var result = await Mediator.Send(query);
            
            if (result == null)
            {
                return NotFound(ApiResponse<TenantDto>.ErrorResponse($"Tenant with ID {tenantId} not found"));
            }
            
            return HandleResult(result, "Current tenant retrieved successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving current tenant for user: {UserId}", CurrentUserId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<TenantDto>.ErrorResponse("An error occurred while retrieving the current tenant"));
        }
    }
}
