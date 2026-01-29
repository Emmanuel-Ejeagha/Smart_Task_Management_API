using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SmartTaskManagementAPI.Application.Common.Exceptions;
using ValidationExp = SmartTaskManagementAPI.Application.Common.Exceptions.ValidationException;

namespace SmartTaskManagementAPI.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
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
        _logger.LogError(exception, "An unhandled exception occured: {Message}", exception.Message);

        var statusCode = GetStatusCode(exception);
        var response = CreateProblemDetails(context, exception, statusCode);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        };

        var json = JsonSerializer.Serialize(response, options);
        await context.Response.WriteAsync(json);
    }

    private static int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            ValidationExp => (int)HttpStatusCode.BadRequest,
            NotFoundException => (int)HttpStatusCode.NotFound,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            ForbiddenAccessException => (int)HttpStatusCode.Forbidden,
            _ => (int)HttpStatusCode.InternalServerError
        };
    }

    private ProblemDetails CreateProblemDetails(HttpContext context, Exception exception, int statusCode)
    {
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = GetTitle(exception),
            Type = GetType(exception),
            Detail = _environment.IsDevelopment() ? exception.Message : GetUserMessage(exception),
            Instance = context.Request.Path
        };

        if (exception is ValidationExp validationException)
        {
            problemDetails.Extensions["errors"] = validationException.Errors;
        }

        if (_environment.IsDevelopment())
        {
            problemDetails.Extensions["traceId"] = context.TraceIdentifier;
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
        }

        return problemDetails;
    }

    private static string GetTitle(Exception exception)
    {
        return exception switch
        {
            ValidationExp => "Validation Error",
            NotFoundException => "Resource Not Found",
            UnauthorizedAccessException => "Unauthorized",
            ForbiddenAccessException => "Forbidden",
            _ => "Internal Server Error"
        };
    }

    private static string GetType(Exception exception)
    {
        return exception switch
        {
            ValidationExp => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            NotFoundException => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            UnauthorizedAccessException => "https://tools.ietf.org/html/rfc7235#section-3.1",
            ForbiddenAccessException => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        };
    }

    private static string GetUserMessage(Exception exception)
    {
        return exception switch
        {
            ValidationExp => "One or more validation errors occurred.",
            NotFoundException => "The requested resource was not found.",
            UnauthorizedAccessException => "You are not authorized to access this resource.",
            ForbiddenAccessException => "You do not have permission to access this resource.",
            _ => "An unexpected error occurred. Please try again later."
        };
    }
}
