using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace SmartTaskManagementAPI.API.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var request = context.Request;

        // Store the original response body stream
        var originalResponseBody = context.Response.Body;

        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            // Log request details
            await LogRequestAsync(context);

            await _next(context);

            stopwatch.Stop();

            // Log response details
            await LogResponseAsync(context, stopwatch.ElapsedMilliseconds);

            // Copy the response body to the original stream
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalResponseBody);
        }
        finally
        {
            context.Response.Body = originalResponseBody;
        }
    }

    private async Task LogRequestAsync(HttpContext context)
    {
        var request = context.Request;

        // Build request log
        var requestLog = new StringBuilder();
        requestLog.AppendLine($"Request {request.Method} {request.Path}{request.QueryString}");
        requestLog.AppendLine($"Content-Type: {request.ContentType}");
        requestLog.AppendLine($"Content-Length: {request.ContentLength}");

        // Log headers (excluding sensitive ones)
        foreach (var header in request.Headers)
        {
            if (!IsSensitiveHeader(header.Key))
            {
                requestLog.AppendLine($"{header.Key}: {GetHeaderValue(header.Value)}");
            }
        }

        // Log body for non-binary requests
        if (request.ContentLength > 0 &&
            request.ContentLength < 1000 &&
            !IsBinaryContent(request.ContentType))
        {
            request.EnableBuffering();
            var bodyReader = new StreamReader(request.Body, Encoding.UTF8, true, 1024, true);
            var body = await bodyReader.ReadToEndAsync();
            request.Body.Seek(0, SeekOrigin.Begin);

            if (!string.IsNullOrEmpty(body))
            {
                requestLog.AppendLine($"Body: {body}");
            }

        }
        _logger.LogInformation("HTTP Request:\n{RequestLog}", requestLog.ToString());
    }

    private async Task LogResponseAsync(HttpContext context, long ElapsedMilliseconds)
    {
        var response = context.Response;

        // Read response body
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        context.Response.Body.Seek(0, SeekOrigin.Begin);

        // Build response log
        var responseLog = new StringBuilder();
        responseLog.AppendLine($"Response {response.StatusCode} in {ElapsedMilliseconds}ms");
        responseLog.AppendLine($"Content-Type: {response.ContentType}");
        responseLog.AppendLine($"Content-Length: {responseBody.Length}");

        // Log headers (excluding sensitive ones)
        foreach (var header in response.Headers)
        {
            if (!IsSensitiveHeader(header.Key))
            {
                responseLog.AppendLine($"{header.Key}: {GetHeaderValue(header.Value)}");
            }
        }

        // Log body for non-binary responses
        if (responseLog.Length > 0 &&
            responseBody.Length < 10000 &&
            !IsBinaryContent(response.ContentType))
        {
            responseLog.AppendLine($"Body: {responseBody}");
        }

        // Log at appropriate level based on status code
        if (response.StatusCode >= 400 && response.StatusCode < 500)
        {
            _logger.LogWarning("Http Response:\n{ResponseLog}", responseLog.ToString());
        }
        else if (response.StatusCode >= 500)
        {
            _logger.LogError("Http Response:\n{ResponseLog}", responseLog.ToString());
        }
        else
        {
            _logger.LogInformation("HTTP Response:\n{ResponseLog}", responseLog.ToString());
        }
    }

    private static bool IsSensitiveHeader(string headerName)
    {
        var sensitiveHeaders = new[]
        {
            "Authorization",
            "Cookie",
            "Set-Cookie",
            "X-API-Key",
            "X-Api-Key",
            "X-Token",
            "X-Access-Token",
            "X-Refresh-Token"
        };

        return sensitiveHeaders.Any(h =>
            string.Equals(h, headerName, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetHeaderValue(StringValues values)
    {
        if (values.Count == 0) return string.Empty;
        if (values.Count == 1) return values.ToString();
        return $"[{string.Join(", ", values)}]";
    }
    
    private static bool IsBinaryContent(string? contextType)
    {
        if (string.IsNullOrEmpty(contextType)) return false;

        var binaryTypes = new[]
        {
            "image/",
            "video/",
            "audio/",
            "application/octet-stream",
            "application/pdf",
            "application/zip",
            "application/gzip"
        };

        return binaryTypes.Any(t => contextType.StartsWith(t, StringComparison.OrdinalIgnoreCase));
    }
}
