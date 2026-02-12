using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Infrastructure.Data;

namespace SmartTaskManagement.Infrastructure.BackgroundJobs;

/// <summary>
/// Command to process due reminders (can be called from API or background job)
/// </summary>
public class ProcessDueRemindersCommand : IRequest<Result>
{
    public int BatchSize { get; set; } = 100;
}

public class ProcessDueRemindersCommandHandler : IRequestHandler<ProcessDueRemindersCommand, Result>
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public ProcessDueRemindersCommandHandler(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IBackgroundJobClient backgroundJobClient)
    {
        _dbContextFactory = dbContextFactory;
        _backgroundJobClient = backgroundJobClient;
    }

    public async Task<Result> Handle(
        ProcessDueRemindersCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            var now = DateTime.UtcNow;
            var dueReminders = await context.Reminders
                .Where(r => r.Status == Domain.Enums.ReminderStatus.Scheduled &&
                           r.ReminderDateUtc <= now)
                .OrderBy(r => r.ReminderDateUtc)
                .Take(request.BatchSize)
                .Select(r => r.Id)
                .ToListAsync(cancellationToken);

            if (!dueReminders.Any())
                return Result.Success();

            // Enqueue batch processing job
            _backgroundJobClient.Enqueue<ReminderJob>(
                job => job.ProcessMultipleRemindersAsync(dueReminders, null!));

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Error processing due reminders: {ex.Message}");
        }
    }
}