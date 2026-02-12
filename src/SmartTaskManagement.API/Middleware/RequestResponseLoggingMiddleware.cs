using System.Text;
using System.Text.Json;

namespace SmartTaskManagement.API.Middleware;

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip logging for health checks and swagger
        if (context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/swagger") ||
            context.Request.Path.StartsWithSegments("/hangfire"))
        {
            await _next(context);
            return;
        }

        // Log request
        var request = await FormatRequest(context.Request);
        _logger.LogInformation("HTTP Request: {Method} {Path} {QueryString} {Body}",
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString,
            request);

        // Copy original response body stream
        var originalBodyStream = context.Response.Body;
        
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var startTime = DateTime.UtcNow;

        try
        {
            await _next(context);
        }
        finally
        {
            // Log response
            var response = await FormatResponse(context.Response);
            var duration = DateTime.UtcNow - startTime;
            
            _logger.LogInformation("HTTP Response: {StatusCode} {ContentType} in {Duration}ms - {Body}",
                context.Response.StatusCode,
                context.Response.ContentType,
                duration.TotalMilliseconds,
                response);

            // Copy the response body to the original stream
            await responseBody.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;
        }
    }

    private static async Task<string> FormatRequest(HttpRequest request)
    {
        request.EnableBuffering();
        
        var body = string.Empty;
        
        // Don't log sensitive data like passwords
        if (!request.Path.StartsWithSegments("/api/auth"))
        {
            using var reader = new StreamReader(
                request.Body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true);
            
            body = await reader.ReadToEndAsync();
            request.Body.Position = 0;
        }

        // Mask sensitive headers
        var headers = request.Headers
            .Where(h => !IsSensitiveHeader(h.Key))
            .ToDictionary(h => h.Key, h => h.Value.ToString());

        return JsonSerializer.Serialize(new
        {
            Headers = headers,
            Body = body
        });
    }

    private static async Task<string> FormatResponse(HttpResponse response)
    {
        response.Body.Seek(0, SeekOrigin.Begin);
        
        string body;
        
        // Don't log large responses or binary data
        if (response.ContentType?.Contains("application/json") == true && 
            response.Body.Length < 1024 * 10) // 10KB limit
        {
            using var reader = new StreamReader(response.Body, leaveOpen: true);
            body = await reader.ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);
        }
        else
        {
            body = $"[{response.ContentType} - {response.Body.Length} bytes]";
        }

        return JsonSerializer.Serialize(new
        {
            StatusCode = response.StatusCode,
            ContentType = response.ContentType,
            Body = body
        });
    }

    private static bool IsSensitiveHeader(string headerName)
    {
        var sensitiveHeaders = new[]
        {
            "Authorization",
            "Cookie",
            "Set-Cookie",
            "X-Api-Key",
            "X-Auth-Token"
        };

        return sensitiveHeaders.Any(h => 
            headerName.Equals(h, StringComparison.OrdinalIgnoreCase));
    }
}

public static class RequestResponseLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestResponseLoggingMiddleware>();
    }
}