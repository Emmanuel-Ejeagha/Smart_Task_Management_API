using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.DependencyInjection;

namespace SmartTaskManagementAPI.Infrastructure.BackgroundJobs;

public class HangfireActivator : JobActivator
{
    private readonly IServiceProvider _serviceProvider;

    public HangfireActivator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override object ActivateJob(Type jobType)
    {
        return _serviceProvider.GetRequiredService(jobType);
    }
}