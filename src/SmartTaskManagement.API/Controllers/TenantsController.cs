using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTaskManagement.API.Controllers.Base;
using SmartTaskManagement.API.Models;
using SmartTaskManagement.Application.Common.Interfaces;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Application.Features.Tenants.Commands.CreateTenant;
using SmartTaskManagement.Application.Features.Tenants.Dtos;
using SmartTaskManagement.Application.Features.Tenants.Queries.GetTenantById;

namespace SmartTaskManagement.API.Controllers;

/// <summary>
/// Manages tenants (Admin only)
/// </summary>
[ApiVersion("1.0")]
[Authorize(Roles = "Admin")]
public class TenantsController : ApiControllerBase
{
    /// <summary>
    /// Get tenant by ID
    /// </summary>
    /// <param name="id">Tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tenant details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TenantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetTenantByIdQuery { Id = id };
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get current tenant (from authenticated user's claim)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current tenant details</returns>
    [HttpGet("current")]
    [ProducesResponseType(typeof(ApiResponse<TenantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken)
    {
        var tenantId = HttpContext.RequestServices.GetRequiredService<ICurrentUserService>().TenantId;
        if (!tenantId.HasValue)
            return NotFound("Current tenant not found");
            
        var query = new GetTenantByIdQuery { Id = tenantId.Value };
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Create a new tenant
    /// </summary>
    /// <param name="command">Tenant creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created tenant ID</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreateTenantCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        if (result.IsSuccess)
        {
            var uri = Url.Action(nameof(GetById), new { id = result.Value }) 
                ?? $"/api/v1/tenants/{result.Value}";
            return Created(uri, result.Value, "Tenant created successfully");
        }
        return HandleResult(result);
    }
}