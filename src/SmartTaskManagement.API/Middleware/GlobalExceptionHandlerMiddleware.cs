using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SmartTaskManagement.Application.Common.Exceptions;
using SmartTaskManagement.Domain.Exceptions;

namespace SmartTaskManagement.API.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var problemDetails = CreateProblemDetails(context, exception);
        
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsJsonAsync(problemDetails, options);
    }

    private ProblemDetails CreateProblemDetails(HttpContext context, Exception exception)
    {
        var problemDetails = new ProblemDetails
        {
            Type = GetProblemType(exception),
            Title = GetProblemTitle(exception),
            Detail = GetProblemDetail(exception),
            Instance = context.Request.Path,
            Status = GetStatusCode(exception)
        };

        // Add additional details for development environment
        if (_env.IsDevelopment())
        {
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
            problemDetails.Extensions["innerException"] = exception.InnerException?.Message;
        }

        // Add custom properties for specific exception types
        switch (exception)
        {
            case ValidationException validationEx:
                problemDetails.Extensions["errors"] = validationEx.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray());
                break;
                
            case NotFoundException notFoundEx:
                problemDetails.Extensions["entityName"] = notFoundEx.EntityName;
                problemDetails.Extensions["entityId"] = notFoundEx.EntityId;
                break;
                
            case UnauthorizedException:
                problemDetails.Extensions["hint"] = "Please check your credentials and try again.";
                break;
                
            case ForbiddenException:
                problemDetails.Extensions["hint"] = "You don't have permission to access this resource.";
                break;
                
            case ConcurrencyException:
                problemDetails.Extensions["hint"] = "The resource was modified by another user. Please refresh and try again.";
                problemDetails.Status = (int)HttpStatusCode.Conflict;
                break;
        }

        // Add correlation ID for tracking
        problemDetails.Extensions["correlationId"] = context.TraceIdentifier;
        
        // Add timestamp
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow.ToString("O");

        return problemDetails;
    }

    private static string GetProblemType(Exception exception)
    {
        return exception switch
        {
            ValidationException => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            NotFoundException => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            UnauthorizedException => "https://tools.ietf.org/html/rfc7235#section-3.1",
            ForbiddenException => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            ConcurrencyException => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        };
    }

    private static string GetProblemTitle(Exception exception)
    {
        return exception switch
        {
            ValidationException => "Validation Error",
            NotFoundException => "Not Found",
            UnauthorizedException => "Unauthorized",
            ForbiddenException => "Forbidden",
            ConcurrencyException => "Concurrency Conflict",
            _ => "Internal Server Error"
        };
    }

    private static string? GetProblemDetail(Exception exception)
    {
        // For production, hide internal error details
        if (exception is not (ValidationException or NotFoundException or UnauthorizedException or ForbiddenException or ConcurrencyException))
        {
            return "An internal server error occurred. Please contact support.";
        }

        return exception.Message;
    }

    private static int? GetStatusCode(Exception exception)
    {
        return exception switch
        {
            ValidationException => (int)HttpStatusCode.BadRequest,
            NotFoundException => (int)HttpStatusCode.NotFound,
            UnauthorizedException => (int)HttpStatusCode.Unauthorized,
            ForbiddenException => (int)HttpStatusCode.Forbidden,
            ConcurrencyException => (int)HttpStatusCode.Conflict,
            _ => (int)HttpStatusCode.InternalServerError
        };
    }
}

public static class GlobalExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}