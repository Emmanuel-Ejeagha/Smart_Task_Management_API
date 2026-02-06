using System.Text.Json;
using SmartTaskManagementAPI.API.Models;

namespace SmartTaskManagementAPI.API.Middleware;

public class ApiResponseWrapperMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiResponseWrapperMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public ApiResponseWrapperMiddleware(
        RequestDelegate next,
        ILogger<ApiResponseWrapperMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Store original response body stream
        var originalBodyStream = context.Response.Body;
        
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);

            // Only wrap responses that are JSON and successful
            if (context.Response.ContentType?.Contains("application/json") == true && 
                context.Response.StatusCode >= 200 && 
                context.Response.StatusCode < 300)
            {
                await WrapResponseAsync(context, responseBody, originalBodyStream);
            }
            else
            {
                await CopyResponseAsync(context, responseBody, originalBodyStream);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in API response middleware");
            await HandleExceptionAsync(context, ex, originalBodyStream);
        }
    }

    private async Task WrapResponseAsync(HttpContext context, MemoryStream responseBody, Stream originalBodyStream)
    {
        responseBody.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(responseBody).ReadToEndAsync();
        
        // Try to parse the existing response
        object? data = null;
        try
        {
            if (!string.IsNullOrWhiteSpace(responseText))
            {
                data = JsonSerializer.Deserialize<object>(responseText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
        }
        catch (JsonException)
        {
            // If it's not valid JSON, use it as-is
            data = responseText;
        }

        // Create API response wrapper
        var apiResponse = new ApiResponse<object>
        {
            Success = true,
            Message = GetSuccessMessage(context),
            Data = data,
            Timestamp = DateTime.UtcNow
        };

        // Serialize the wrapped response
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        };

        var jsonResponse = JsonSerializer.Serialize(apiResponse, jsonOptions);
        
        // Reset the response
        context.Response.Body = originalBodyStream;
        context.Response.ContentLength = null; // Let middleware set it
        
        await context.Response.WriteAsync(jsonResponse);
    }

    private async Task CopyResponseAsync(HttpContext context, MemoryStream responseBody, Stream originalBodyStream)
    {
        responseBody.Seek(0, SeekOrigin.Begin);
        context.Response.Body = originalBodyStream;
        await responseBody.CopyToAsync(originalBodyStream);
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, Stream originalBodyStream)
    {
        context.Response.Body = originalBodyStream;
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var apiResponse = new ApiResponse<object>
        {
            Success = false,
            Message = "An unexpected error occurred",
            Timestamp = DateTime.UtcNow
        };

        if (_environment.IsDevelopment())
        {
            apiResponse.Errors.Add(new ApiError(
                "UNEXPECTED_ERROR",
                exception.ToString()));
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        };

        var jsonResponse = JsonSerializer.Serialize(apiResponse, jsonOptions);
        await context.Response.WriteAsync(jsonResponse);
    }

    private string GetSuccessMessage(HttpContext context)
    {
        return context.Request.Method switch
        {
            "GET" => "Request processed successfully",
            "POST" => "Resource created successfully",
            "PUT" => "Resource updated successfully",
            "PATCH" => "Resource updated successfully",
            "DELETE" => "Resource deleted successfully",
            _ => "Operation completed successfully"
        };
    }
}