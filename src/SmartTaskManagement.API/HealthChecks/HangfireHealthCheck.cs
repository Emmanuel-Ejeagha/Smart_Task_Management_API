using Hangfire;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SmartTaskManagement.API.HealthChecks;

public class HangfireHealthCheck : IHealthCheck
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<HangfireHealthCheck> _logger;

    public HangfireHealthCheck(IBackgroundJobClient backgroundJobClient, ILogger<HangfireHealthCheck> logger)
    {
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Test connectivity by creating a dummy job (will be deleted)
            var jobId = _backgroundJobClient.Enqueue(() => DummyJob());
            
            // Immediately delete it to avoid clutter
            BackgroundJob.Delete(jobId);
            
            return HealthCheckResult.Healthy("Hangfire is responsive.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Hangfire health check failed – marking as Degraded.");
            // Return Degraded instead of Unhealthy – allows startup to proceed
            return HealthCheckResult.Degraded("Hangfire is not yet ready.");
        }
    }

    // Dummy job – never actually executed
    public static void DummyJob() { }
}