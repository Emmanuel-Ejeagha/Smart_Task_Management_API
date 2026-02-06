using System.Diagnostics;

namespace SmartTaskManagementAPI.API.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var request = context.Request;
        
        // Log request start
        _logger.LogInformation(
            "HTTP {Method} {Path} started. User: {User}, Tenant: {Tenant}",
            request.Method,
            request.Path,
            context.User.Identity?.Name ?? "Anonymous",
            context.User.FindFirst("TenantId")?.Value ?? "No-Tenant");

        try
        {
            await _next(context);
            stopwatch.Stop();

            var response = context.Response;
            
            // Log request completion
            _logger.LogInformation(
                "HTTP {Method} {Path} completed with status {StatusCode} in {ElapsedMilliseconds}ms. User: {User}",
                request.Method,
                request.Path,
                response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                context.User.Identity?.Name ?? "Anonymous");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex,
                "HTTP {Method} {Path} failed with error in {ElapsedMilliseconds}ms. User: {User}",
                request.Method,
                request.Path,
                stopwatch.ElapsedMilliseconds,
                context.User.Identity?.Name ?? "Anonymous");
            
            throw;
        }
    }
}