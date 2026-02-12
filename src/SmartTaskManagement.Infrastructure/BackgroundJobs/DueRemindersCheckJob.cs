using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartTaskManagement.Infrastructure.Data;

namespace SmartTaskManagement.Infrastructure.BackgroundJobs;

public class DueRemindersCheckJob
{
    private readonly ILogger<DueRemindersCheckJob> _logger;
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public DueRemindersCheckJob(
        ILogger<DueRemindersCheckJob> logger,
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IBackgroundJobClient backgroundJobClient)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _backgroundJobClient = backgroundJobClient;
    }

    [AutomaticRetry(Attempts = 2)]
    public async Task CheckDueRemindersAsync(IJobCancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Checking for due reminders");

            using var context = _dbContextFactory.CreateDbContext();
            
            // Get reminders that are due (scheduled and reminder date <= now)
            var now = DateTime.UtcNow;
            var dueReminders = await context.Reminders
                .Where(r => r.Status == Domain.Enums.ReminderStatus.Scheduled &&
                           r.ReminderDateUtc <= now)
                .OrderBy(r => r.ReminderDateUtc)
                .Take(100) // Process in batches
                .ToListAsync(cancellationToken.ShutdownToken);

            if (!dueReminders.Any())
            {
                _logger.LogInformation("No due reminders found");
                return;
            }

            _logger.LogInformation("Found {Count} due reminders", dueReminders.Count);

            // Trigger each reminder
            foreach (var reminder in dueReminders)
            {
                if (cancellationToken.ShutdownToken.IsCancellationRequested)
                    break;

                // Enqueue reminder processing
                _backgroundJobClient.Enqueue<ReminderJob>(
                    job => job.ProcessReminderAsync(reminder.Id, JobCancellationToken.Null));
            }

            _logger.LogInformation("Enqueued {Count} reminder jobs", dueReminders.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking due reminders");
            throw;
        }
    }

    [AutomaticRetry(Attempts = 0)] // Don't retry cleanup jobs
    public async Task CleanupOldRemindersAsync(IJobCancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Cleaning up old reminders");

            using var context = _dbContextFactory.CreateDbContext();
            
            var cutoffDate = DateTime.UtcNow.AddMonths(-6); // Keep reminders for 6 months
            
            var oldReminders = await context.Reminders
                .Where(r => r.CreatedAtUtc < cutoffDate &&
                           (r.Status == Domain.Enums.ReminderStatus.Triggered ||
                            r.Status == Domain.Enums.ReminderStatus.Cancelled))
                .ToListAsync(cancellationToken.ShutdownToken);

            if (!oldReminders.Any())
            {
                _logger.LogInformation("No old reminders to cleanup");
                return;
            }

            _logger.LogInformation("Found {Count} old reminders to cleanup", oldReminders.Count);
            
            // Soft delete old reminders
            foreach (var reminder in oldReminders)
            {
                reminder.MarkAsDeleted("system-cleanup");
            }

            await context.SaveChangesAsync(cancellationToken.ShutdownToken);
            
            _logger.LogInformation("Cleaned up {Count} old reminders", oldReminders.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old reminders");
            throw;
        }
    }

    public async Task RescheduleMissedRemindersAsync(IJobCancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Checking for missed reminders");

            using var context = _dbContextFactory.CreateDbContext();
            
            var now = DateTime.UtcNow;
            var fiveMinutesAgo = now.AddMinutes(-5);
            var oneHourAgo = now.AddHours(-1);
            
            // Find reminders that should have been triggered but weren't
            var missedReminders = await context.Reminders
                .Where(r => r.Status == Domain.Enums.ReminderStatus.Scheduled &&
                           r.ReminderDateUtc >= oneHourAgo &&
                           r.ReminderDateUtc <= fiveMinutesAgo)
                .ToListAsync(cancellationToken.ShutdownToken);

            if (!missedReminders.Any())
            {
                _logger.LogInformation("No missed reminders found");
                return;
            }

            _logger.LogInformation("Found {Count} missed reminders", missedReminders.Count);

            // Trigger missed reminders immediately
            foreach (var reminder in missedReminders)
            {
                if (cancellationToken.ShutdownToken.IsCancellationRequested)
                    break;

                _backgroundJobClient.Enqueue<ReminderJob>(
                    job => job.ProcessReminderAsync(reminder.Id, JobCancellationToken.Null));
            }

            _logger.LogInformation("Rescheduled {Count} missed reminders", missedReminders.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rescheduling missed reminders");
            throw;
        }
    }
}