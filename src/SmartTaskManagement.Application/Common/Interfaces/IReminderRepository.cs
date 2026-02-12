using SmartTaskManagement.Domain.Entities;

namespace SmartTaskManagement.Application.Common.Interfaces;

/// <summary>
/// Reminder-specific repository interface
/// </summary>
public interface IReminderRepository : IRepository<Reminder>
{
    /// <summary>
    /// Get reminders by work item ID
    /// </summary>
    Task<IReadOnlyList<Reminder>> GetByWorkItemIdAsync(
        Guid workItemId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get due reminders (reminder date <= current time)
    /// </summary>
    Task<IReadOnlyList<Reminder>> GetDueRemindersAsync(
        DateTime currentTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get scheduled reminders (not triggered, not cancelled, not failed)
    /// </summary>
    Task<IReadOnlyList<Reminder>> GetScheduledRemindersAsync(
        CancellationToken cancellationToken = default);
}