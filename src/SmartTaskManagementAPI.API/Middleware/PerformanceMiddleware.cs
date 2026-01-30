using System.Diagnostics;

namespace SmartTaskManagementAPI.API.Middleware;

public class PerformanceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMiddleware> _logger;
    private readonly long _warningThresholdMs;

    public PerformanceMiddleware(
        RequestDelegate next,
        ILogger<PerformanceMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _warningThresholdMs = configuration.GetValue<long>("Performance:WarningThresholdsMs", 1000);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var elapsedMs = stopwatch.ElapsedMilliseconds;

            if (elapsedMs > _warningThresholdMs)
            {
                _logger.LogWarning(
                    "Performance Warning: Request {Method} {Path} took {ElapsedMs}ms (Threshold: {ThresholdMs}ms)",
                    context.Request.Method,
                    context.Request.Path,
                    elapsedMs,
                    _warningThresholdMs);
            }
            else
            {
                _logger.LogDebug(
                    "Request {Method} {Path} complete in {ElapsedMs}ms",
                    context.Request.Method,
                    context.Request.Path,
                    elapsedMs);
            }

            // Add performance header to response
            context.Response.Headers["X-Response-Time"] = $"{elapsedMs}ms";
        }
    }
}
