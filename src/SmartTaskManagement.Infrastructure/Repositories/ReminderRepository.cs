using Microsoft.EntityFrameworkCore;
using SmartTaskManagement.Application.Common.Interfaces;
using SmartTaskManagement.Domain.Entities;
using SmartTaskManagement.Domain.Enums;
using SmartTaskManagement.Infrastructure.Data;

namespace SmartTaskManagement.Infrastructure.Repositories;

public class ReminderRepository : GenericRepository<Reminder>, IReminderRepository
{
    public ReminderRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Reminder>> GetByWorkItemIdAsync(
        Guid workItemId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Reminders
            .Where(r => r.WorkItemId == workItemId)
            .OrderBy(r => r.ReminderDateUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Reminder>> GetDueRemindersAsync(
        DateTime currentTime,
        CancellationToken cancellationToken = default)
    {
        return await _context.Reminders
            .Include(r => r.WorkItem)
            .Where(r => r.Status == ReminderStatus.Scheduled &&
                       r.ReminderDateUtc <= currentTime)
            .OrderBy(r => r.ReminderDateUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Reminder>> GetScheduledRemindersAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Reminders
            .Where(r => r.Status == ReminderStatus.Scheduled)
            .OrderBy(r => r.ReminderDateUtc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get reminders that need to be triggered (due now)
    /// </summary>
    public async Task<IReadOnlyList<Reminder>> GetRemindersToTriggerAsync(
        int batchSize = 100,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var fiveMinutesAgo = now.AddMinutes(-5); // Buffer for clock drift

        return await _context.Reminders
            .Include(r => r.WorkItem)
            .Where(r => r.Status == ReminderStatus.Scheduled &&
                       r.ReminderDateUtc >= fiveMinutesAgo &&
                       r.ReminderDateUtc <= now)
            .OrderBy(r => r.ReminderDateUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get failed reminders that can be retried
    /// </summary>
    public async Task<IReadOnlyList<Reminder>> GetFailedRemindersForRetryAsync(
        int maxRetryCount = 3,
        TimeSpan retryDelay = default,
        CancellationToken cancellationToken = default)
    {
        if (retryDelay == default)
            retryDelay = TimeSpan.FromMinutes(5);

        var retryCutoff = DateTime.UtcNow.Add(-retryDelay);

        // This query assumes we have a retry count field
        // For now, we'll return empty until we add retry logic
        return await Task.FromResult(new List<Reminder>());
    }
}