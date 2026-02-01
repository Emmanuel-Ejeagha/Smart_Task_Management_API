using System;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartTaskManagementAPI.API.Models;
using SmartTaskManagementAPI.Application.Common.Models;

namespace SmartTaskManagementAPI.API.Controllers.Base;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
[Consumes("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    private IMediator? _mediator;
    private ILogger<ApiControllerBase>? _logger;

    protected IMediator Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<IMediator>();
    protected ILogger<ApiControllerBase> Logger => _logger ??= HttpContext.RequestServices.GetRequiredService<ILogger<ApiControllerBase>>();

    protected Guid CurrentUserId
    {
        get
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }
    }

    protected Guid CurrentTenantId
    {
        get
        {
            var tenantIdClaim = User.FindFirstValue("TenantId");
            return Guid.TryParse(tenantIdClaim, out var tenantId) ? tenantId : Guid.Empty;
        }
    }

    protected bool IsAdmin => User.IsInRole("Admin");
    protected bool IsUser => User.IsInRole("User");

    protected ActionResult<ApiResponse<T>> HandleResult<T>(T result, string successMessage = "")
    {
        if (result == null)
        {
            return NotFound(ApiResponse<T>.ErrorResponse("Resource not found"));
        }

        return Ok(ApiResponse<T>.SuccessResponse(result, successMessage));
    }

    protected ActionResult<ApiResponse> HandleCommandResult(Unit result, string successMessage = "")
    {
        return Ok(ApiResponse.SuccessResponse(successMessage));
    }

    protected ActionResult<PaginatedApiResponse<T>> HandlePaginatedResult<T>(PaginatedResult<T> result, string successMessage = "")
    {
        return Ok(PaginatedApiResponse<T>.SuccessResponse(
            result.Items,
            result.PageNumber,
            result.PageSize,
            result.TotalCount,
            successMessage));
    }

    protected ActionResult<ApiResponse<T>> CreatedResult<T>(T result, string routeName, object routeValues, string successMessage = "Resource created successfully")
    {
        return CreatedAtRoute(routeName, routeValues, ApiResponse<T>.SuccessResponse(result, successMessage));
    }

    protected ActionResult<ApiResponse<T>> CreatedResult<T>(T result, string actionName, string controllerName, object routeValues, string successMessage = "Resource created successfully")
    {
        return CreatedAtAction(actionName, controllerName, routeValues, ApiResponse<T>.SuccessResponse(result, successMessage));
    }
}
