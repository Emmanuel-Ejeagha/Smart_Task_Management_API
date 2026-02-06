using System.Net;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using SmartTaskManagementAPI.API.Models;
using SmartTaskManagementAPI.Application.Common.Exceptions;

namespace SmartTaskManagementAPI.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var apiResponse = new ApiResponse<object>
        {
            Success = false,
            Timestamp = DateTime.UtcNow
        };

        switch (exception)
        {
            case ValidationException validationEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                apiResponse.Message = "Validation failed";
                apiResponse.Errors = validationEx.Errors
                    .SelectMany(e => e.Value.Select(v => new ApiError(
                        "VALIDATION_ERROR",
                        v,
                        e.Key)))
                    .ToList();
                _logger.LogWarning(validationEx, "Validation failed for request: {Path}", context.Request.Path);
                break;

            case NotFoundException notFoundEx:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                apiResponse.Message = notFoundEx.Message;
                _logger.LogWarning(notFoundEx, "Resource not found: {Message}", notFoundEx.Message);
                break;

            case UnauthorizedAccessException unauthorizedEx:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                apiResponse.Message = "Unauthorized access";
                _logger.LogWarning(unauthorizedEx, "Unauthorized access: {Message}", unauthorizedEx.Message);
                break;

            case InvalidOperationException invalidOpEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                apiResponse.Message = invalidOpEx.Message;
                _logger.LogWarning(invalidOpEx, "Invalid operation: {Message}", invalidOpEx.Message);
                break;

            case SecurityTokenException securityTokenEx:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                apiResponse.Message = "Invalid or expired token";
                _logger.LogWarning(securityTokenEx, "Security token error: {Message}", securityTokenEx.Message);
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                apiResponse.Message = _environment.IsDevelopment() 
                    ? exception.Message 
                    : "An internal server error occurred";
                
                if (_environment.IsDevelopment())
                {
                    apiResponse.Errors.Add(new ApiError(
                        "INTERNAL_ERROR",
                        exception.ToString()));
                }
                
                _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
                break;
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        };

        var json = JsonSerializer.Serialize(apiResponse, options);
        await response.WriteAsync(json);
    }
}