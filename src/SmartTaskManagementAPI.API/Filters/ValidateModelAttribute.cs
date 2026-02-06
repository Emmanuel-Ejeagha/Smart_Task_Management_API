using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SmartTaskManagementAPI.API.Models;

namespace SmartTaskManagementAPI.API.Filters;

public class ValidateModelAttribute : ActionFilterAttribute
{
    private readonly ILogger<ValidateModelAttribute> _logger;

    public ValidateModelAttribute(ILogger<ValidateModelAttribute> logger)
    {
        _logger = logger;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .SelectMany(e => e.Value!.Errors.Select(error => new ApiError(
                    "MODEL_VALIDATION",
                    error.ErrorMessage,
                    e.Key)))
                .ToList();

            var response = ApiResponse.ErrorResponse("Model validation failed", errors);
            
            context.Result = new BadRequestObjectResult(response);
            
            _logger.LogWarning("Model validation failed for request: {Path}", context.HttpContext.Request.Path);
        }
        
        base.OnActionExecuting(context);
    }
}