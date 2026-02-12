using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTaskManagement.API.Filters;
using SmartTaskManagement.API.Models;

namespace SmartTaskManagement.API.Controllers.Base;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
[Consumes("application/json")]
[ServiceFilter(typeof(ModelValidationFilter))]
[ServiceFilter(typeof(AuditLogActionFilter))]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _mediator;

    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    /// <summary>
    /// Creates a successful API response
    /// </summary>
    protected IActionResult Success<T>(T data, string? message = null)
    {
        var response = new ApiResponse<T>
        {
            Success = true,
            Message = message ?? "Operation completed successfully",
            Data = data,
            Timestamp = DateTime.UtcNow
        };

        return Ok(response);
    }

    /// <summary>
    /// Creates a successful API response without data
    /// </summary>
    protected IActionResult Success(string? message = null)
    {
        var response = new ApiResponse<object>
        {
            Success = true,
            Message = message ?? "Operation completed successfully",
            Data = null,
            Timestamp = DateTime.UtcNow
        };

        return Ok(response);
    }

    /// <summary>
    /// Creates a paginated API response
    /// </summary>
    protected IActionResult Paginated<T>(PaginatedResponse<T> data, string? message = null)
    {
        var response = new ApiResponse<PaginatedResponse<T>>
        {
            Success = true,
            Message = message ?? "Data retrieved successfully",
            Data = data,
            Timestamp = DateTime.UtcNow
        };

        return Ok(response);
    }

    /// <summary>
    /// Creates a created (201) API response
    /// </summary>
    protected IActionResult Created<T>(string uri, T data, string? message = null)
    {
        var response = new ApiResponse<T>
        {
            Success = true,
            Message = message ?? "Resource created successfully",
            Data = data,
            Timestamp = DateTime.UtcNow
        };

        return Created(uri, response);
    }

    /// <summary>
    /// Creates a no content (204) response
    /// </summary>
    protected IActionResult NoContent(string? message = null)
    {
        var response = new ApiResponse<object>
        {
            Success = true,
            Message = message ?? "Operation completed successfully",
            Data = null,
            Timestamp = DateTime.UtcNow
        };

        return base.NoContent();
    }

    /// <summary>
    /// Handles application result responses
    /// </summary>
    protected IActionResult HandleResult<T>(Application.Common.Models.Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Success(result.Value);
        }

        return HandleFailure(result);
    }

    /// <summary>
    /// Handles application result responses without data
    /// </summary>
    protected IActionResult HandleResult(Application.Common.Models.Result result)
    {
        if (result.IsSuccess)
        {
            return Success();
        }

        return HandleFailure(result);
    }

    /// <summary>
    /// Handles paginated result responses
    /// </summary>
    protected IActionResult HandlePaginatedResult<T>(Application.Common.Models.Result<Application.Common.Models.PaginatedResult<T>> result)
    {
        if (result.IsSuccess && result.Value != null)
        {
            var paginatedResponse = new PaginatedResponse<T>
            {
                Items = result.Value.Items,
                PageNumber = result.Value.PageNumber,
                PageSize = result.Value.PageSize,
                TotalCount = result.Value.TotalCount,
                TotalPages = result.Value.TotalPages,
                HasPreviousPage = result.Value.HasPreviousPage,
                HasNextPage = result.Value.HasNextPage
            };

            return Paginated(paginatedResponse);
        }

        return HandleFailure(result);
    }

    private IActionResult HandleFailure<T>(Application.Common.Models.Result<T> result)
    {
        if (result.ValidationErrors.Any())
        {
            var errors = result.ValidationErrors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            var problemDetails = new ValidationProblemDetails(errors)
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Validation Error",
                Status = StatusCodes.Status400BadRequest,
                Detail = "One or more validation errors occurred.",
                Instance = HttpContext.Request.Path
            };

            problemDetails.Extensions["traceId"] = HttpContext.TraceIdentifier;
            problemDetails.Extensions["timestamp"] = DateTime.UtcNow.ToString("O");

            return BadRequest(problemDetails);
        }

        return BadRequest(new ApiResponse<object>
        {
            Success = false,
            Message = result.Error,
            Data = null,
            Timestamp = DateTime.UtcNow,
            ErrorCode = GetErrorCodeFromError(result.Error)
        });
    }

    private IActionResult HandleFailure(Application.Common.Models.Result result)
    {
        if (result.ValidationErrors.Any())
        {
            var errors = result.ValidationErrors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            var problemDetails = new ValidationProblemDetails(errors)
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Validation Error",
                Status = StatusCodes.Status400BadRequest,
                Detail = "One or more validation errors occurred.",
                Instance = HttpContext.Request.Path
            };

            problemDetails.Extensions["traceId"] = HttpContext.TraceIdentifier;
            problemDetails.Extensions["timestamp"] = DateTime.UtcNow.ToString("O");

            return BadRequest(problemDetails);
        }

        return BadRequest(new ApiResponse<object>
        {
            Success = false,
            Message = result.Error,
            Data = null,
            Timestamp = DateTime.UtcNow,
            ErrorCode = GetErrorCodeFromError(result.Error)
        });
    }

    private static string? GetErrorCodeFromError(string error)
    {
        if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
            return "NOT_FOUND";
        
        if (error.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) || 
            error.Contains("access denied", StringComparison.OrdinalIgnoreCase))
            return "UNAUTHORIZED";
        
        if (error.Contains("validation", StringComparison.OrdinalIgnoreCase))
            return "VALIDATION_ERROR";
        
        if (error.Contains("conflict", StringComparison.OrdinalIgnoreCase))
            return "CONFLICT";
        
        return "GENERIC_ERROR";
    }
}