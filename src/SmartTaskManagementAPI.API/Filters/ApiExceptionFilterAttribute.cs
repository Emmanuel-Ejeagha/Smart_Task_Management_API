using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SmartTaskManagementAPI.API.Models;
using SmartTaskManagementAPI.Application.Common.Exceptions;

namespace SmartTaskManagementAPI.API.Filters;

public class ApiExceptionFilterAttribute : ExceptionFilterAttribute
{
    private readonly IDictionary<Type, Action<ExceptionContext>> _exceptionHandlers;
    private readonly ILogger<ApiExceptionFilterAttribute> _logger;

    public ApiExceptionFilterAttribute(ILogger<ApiExceptionFilterAttribute> logger)
    {
        _logger = logger;
        
        // Register known exception types and handlers
        _exceptionHandlers = new Dictionary<Type, Action<ExceptionContext>>
        {
            { typeof(ValidationException), HandleValidationException },
            { typeof(NotFoundException), HandleNotFoundException },
            { typeof(UnauthorizedAccessException), HandleUnauthorizedAccessException }
        };
    }

    public override void OnException(ExceptionContext context)
    {
        HandleException(context);
        base.OnException(context);
    }

    private void HandleException(ExceptionContext context)
    {
        Type type = context.Exception.GetType();
        if (_exceptionHandlers.ContainsKey(type))
        {
            _exceptionHandlers[type].Invoke(context);
            return;
        }

        HandleUnknownException(context);
    }

    private void HandleValidationException(ExceptionContext context)
    {
        var exception = (ValidationException)context.Exception;
        
        var errors = exception.Errors
            .SelectMany(e => e.Value.Select(v => new ApiError("VALIDATION_ERROR", v, e.Key)))
            .ToList();

        var response = ApiResponse.ErrorResponse("Validation failed", errors);
        
        context.Result = new BadRequestObjectResult(response);
        context.ExceptionHandled = true;
        
        _logger.LogWarning(exception, "Validation failed for request");
    }

    private void HandleNotFoundException(ExceptionContext context)
    {
        var exception = (NotFoundException)context.Exception;
        
        var response = ApiResponse.ErrorResponse(exception.Message);
        
        context.Result = new NotFoundObjectResult(response);
        context.ExceptionHandled = true;
        
        _logger.LogWarning(exception, "Resource not found");
    }

    private void HandleUnauthorizedAccessException(ExceptionContext context)
    {
        var exception = (UnauthorizedAccessException)context.Exception;
        
        var response = ApiResponse.ErrorResponse("Unauthorized access");
        
        context.Result = new UnauthorizedObjectResult(response);
        context.ExceptionHandled = true;
        
        _logger.LogWarning(exception, "Unauthorized access");
    }

    private void HandleUnknownException(ExceptionContext context)
    {
        var exception = context.Exception;
        
        var response = ApiResponse.ErrorResponse("An unexpected error occurred");
        
        context.Result = new ObjectResult(response)
        {
            StatusCode = StatusCodes.Status500InternalServerError
        };
        context.ExceptionHandled = true;
        
        _logger.LogError(exception, "Unhandled exception occurred");
    }
}