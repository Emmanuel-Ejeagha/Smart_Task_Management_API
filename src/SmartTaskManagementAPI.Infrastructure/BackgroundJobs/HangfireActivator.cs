using Hangfire;
using Microsoft.Extensions.DependencyInjection;

namespace SmartTaskManagementAPI.Infrastructure.BackgroundJobs;

public class HangfireActivator : JobActivator
{
    private readonly IServiceProvider _serviceProvider;

    public HangfireActivator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public override object ActivateJob(Type jobType)
    {
        return _serviceProvider.GetRequiredService(jobType);
    }

    public override JobActivatorScope BeginScope(JobActivatorContext context)
    {
        return new HangfireActivatorScope(_serviceProvider.CreateAsyncScope());
    }
}

public class HangfireActivatorScope : JobActivatorScope
{
    private readonly IServiceScope _serviceScope;

    public HangfireActivatorScope(IServiceScope serviceScope)
    {
        _serviceScope = serviceScope ?? throw new ArgumentNullException(nameof(serviceScope));
    }

    public override object Resolve(Type type)
    {
        return _serviceScope.ServiceProvider.GetRequiredService(type);
    }

    public override void DisposeScope()
    {
        _serviceScope.Dispose();
    }
}
