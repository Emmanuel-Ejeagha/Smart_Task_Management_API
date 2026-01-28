using Task = System.Threading.Tasks.Task;
using System.Collections.Concurrent;
using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartTaskManagementAPI.Application.Common.Interfaces;
using SmartTaskManagementAPI.Domain.Entities;
using SmartTaskManagementAPI.Domain.Enums;
using TaskEntity = SmartTaskManagementAPI.Domain.Entities.Task;

namespace SmartTaskManagementAPI.Infrastructure.BackgroundJobs;

public class TaskReminderJob
{
    private readonly ILogger<TaskReminderJob> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public TaskReminderJob(
        ILogger<TaskReminderJob> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    [AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    [DisableConcurrentExecution(timeoutInSeconds: 60)]
    public async Task SendTaskRemindersAsync(PerformContext? context = null)
    {
        var jobId = context?.BackgroundJob.Id ?? "unknown";
        _logger.LogInformation(
            "Stating TaskReminderJob with ID: {JobId} at {UtcNow}",
            jobId, DateTime.UtcNow);

        try
        {
            using var scope = _serviceScopeFactory.CreateAsyncScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            // Get all tasks due for reminder
            var taskDueForReminder = await unitOfWork.Tasks.GetTasksDueForReminderAsync();

            _logger.LogInformation(
                "Found {TaskCount} tasks due for reminder in Job: {JobId}",
                taskDueForReminder.Count(), jobId);

            if (!taskDueForReminder.Any())
            {
                _logger.LogInformation("No tasks require reminders in Job: {JobId}", jobId);
                return;
            }

            // Group tasks by tenant for batch processing
            var tasksByTenant = taskDueForReminder
                .GroupBy(t => t.TenantId)
                .ToList();

            var successfulReminders = new ConcurrentBag<Guid>();
            var failedReminders = new ConcurrentBag<(Guid TaskId, string Error)>();

            // Process each tenant's tasks in parallel
            await Parallel.ForEachAsync(tasksByTenant, async (tenantGroup, cancellationToken) =>
            {
                try
                {
                    await ProcessTenantTasksAsync(
                        tenantGroup.Key,
                        tenantGroup.ToList(),
                        unitOfWork,
                        jobId,
                        successfulReminders,
                        failedReminders,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error processing tenant {TenantId} in Job: {JobId}",
                        tenantGroup.Key, jobId);

                    foreach (var task in tenantGroup)
                    {
                        failedReminders.Add((task.Id, $"Tenant processing failed: , {ex.Message}"));
                    }
                }
            });

            // Save all changes
            await unitOfWork.SaveChangesAsync();

            // Log summary
            _logger.LogInformation(
                "TaskReminderJob {JobId} completed. Successful: {SuccessCount}, Failed: {FailedCount}",
                jobId, successfulReminders.Count, failedReminders.Count);

            if (failedReminders.Any())
            {
                foreach (var (taskId, error) in failedReminders.Take(10))
                {
                    _logger.LogWarning("Failed to send reminder for Task {TaskId}: {Error}", taskId, error);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error in TaskReminderJob {JobId}", jobId);

            throw; // Hangfire will retry based on the retry policy
        }
    }

    private async Task ProcessTenantTasksAsync(
        Guid tenantId,
        List<TaskEntity> tasks,
        IUnitOfWork unitOfWork,
        string jobId,
        ConcurrentBag<Guid> successfulReminders,
        ConcurrentBag<(Guid TaskId, string Error)> failedReminders,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get tenant info
            var tenant = await unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
            if (tenant == null || !tenant.IsActive)
            {
                _logger.LogWarning("Tenant {TenantId} not found or inactive Job: {JobId}", tenantId, jobId);
                return;
            }

            // Get users for this tenant
            var users = await unitOfWork.User.GetUsersByTenantAsync(tenantId, cancellationToken);
            var userDict = users.ToDictionary(u => u.Id);

            foreach (var task in tasks)
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    await ProcessSingleTaskAsync(
                        task,
                        tenant,
                        userDict,
                        unitOfWork,
                        jobId,
                        successfulReminders,
                        failedReminders,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    failedReminders.Add((task.Id, $"Task processing error: {ex.Message}"));
                    _logger.LogError(ex, "Error processing task {TaskId} in Job: {JobId}", task.Id, jobId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing tenant {TenantId} tasks in Job: {JobId}", tenantId, jobId);
            throw;
        }
    }

    private async Task ProcessSingleTaskAsync(
        TaskEntity task,
        Tenant tenant,
        Dictionary<Guid, User> userDict,
        IUnitOfWork unitOfWork,
        string jobId,
        ConcurrentBag<Guid> successfulReminders,
        ConcurrentBag<(Guid TaskId, string Error)> failedReminders,
        CancellationToken cancellationToken)
    {
        // Check if taskcreator still exists and is active
        if (!task.CreatedBy.HasValue || !userDict.TryGetValue(task.CreatedBy.Value, out var creator))
        {
            _logger.LogWarning("Task {TaskId} creator not found in tenant {TenantId}", task.Id, tenant.Id);
            failedReminders.Add((task.Id, "Task creator not found"));
            return;
        }

        if (!creator.IsActive)
        {
            _logger.LogWarning("Task {TaskId} creator is inactive", task.Id);
            failedReminders.Add((task.Id, "Task creator is not active"));
            return;
        }

        // Prepare reminder data
        var reminderData = new
        {
            TaskId = task.Id,
            TaskTitle = task.Title,
            DueDate = task.DueDate?.ToString("yyyy-MM-dd HH:mm"),
            Priority = task.Priority.GetDisplayName(),
            Status = task.Status.GetDisplayName(),
            CreatorName = creator.GetFullName(),
            CreatorEmail = creator.Email,
            TenantName = tenant.Name,
            ReminderSentAt = DateTime.UtcNow
        };

        try
        {
            await SimulateEmailSendingAsync(reminderData, cancellationToken);

            // Clear reminder date and mark as updated
            task.Update(
                task.Title,
                task.Description,
                task.Priority,
                task.DueDate,
                null, // Clear reminder date
                Guid.Empty); // System user

            unitOfWork.Tasks.Update(task);
            successfulReminders.Add(task.Id);

            _logger.LogInformation(
                "Reminder sent for task {TaskId} to {UserEmail} in Job: {JobId}", task.Id, creator.Email, jobId);
        }
        catch (Exception ex)
        {
            failedReminders.Add((task.Id, $"Email sending failed: {ex.Message}"));
            _logger.LogError(ex,
                "Failed to send reminder for task {TaskId} to {UserEmail} in Job: {JobId}",
                    task.Id, creator.Email, jobId);
        }
    }
    private async Task SimulateEmailSendingAsync(object reminderData, CancellationToken cancellationToken)
    {
        // Simulate email sending delay
        await System.Threading.Tasks.Task.Delay(100, cancellationToken);

        // In production, implement actual email sending here
        // Example using SendGrid, MailKit, etc.
        /*
        var emailService = _serviceScopeFactory.CreateScope()
            .ServiceProvider.GetRequiredService<IEmailService>();
        
        await emailService.SendTaskReminderAsync(
            creator.Email,
            "Task Reminder",
            $"Task '{task.Title}' is due on {task.DueDate}");
        */

        // For now, we just log the email content
        _logger.LogDebug("Simulated email sending with data: {@ReminderData}", reminderData);
    }

    [AutomaticRetry(Attempts = 3)]
    [DisableConcurrentExecution(timeoutInSeconds: 30)]
    public async Task SendImmediateTaskReminderAsync(Guid taskId, PerformContext? context = null)
    {
        var jobId = context?.BackgroundJob?.Id ?? "unknown";

        using var scope = _serviceScopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var task = await unitOfWork.Tasks.GetByIdAsync(taskId);
        if (task == null || task.IsDeleted)
        {
            _logger.LogWarning("Task {TaskId} not found for immediate reminder in Job: {JobId}", taskId, jobId);
            return;
        }

        if (task.Status == TasksStatus.Done || task.Status == TasksStatus.Archived)
        {
            _logger.LogWarning("Task {TaskId} is {Status}, skipping reminder in Job: {JobId}",
                taskId, task.Status, jobId);
            return;
        }

        // Get task creator
        var creator = await unitOfWork.User.GetByIdAsync(task.CreatedBy ?? Guid.Empty);
        if (creator == null || !creator.IsActive)
        {
            _logger.LogWarning("Task {TaskId} creator not found/inactive in Job: {JobId}", taskId, jobId);
            return;
        }

        // Get tenant
        var tenant = await unitOfWork.Tenants.GetByIdAsync(task.TenantId);
        if (tenant == null || !tenant.IsActive)
        {
            _logger.LogWarning("Task {TaskId} tenant not found/inactive in Job: {JobId},", taskId, jobId);
            return;
        }

        try
        {
            // Send immediate reminder
            await SimulateEmailSendingAsync(new
            {
                TaskId = task.Id,
                TaskTitle = task.Title,
                ImmediateReminder = true,
                SentAt = DateTime.UtcNow
            }, CancellationToken.None);

            _logger.LogInformation(
                "Immediate reminder sent for task {TaskId} in Job: {JobId}",
                task.Id, jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                 "Failed to send immediate reminder for task {TaskId} in Job: {JobId}",
                 taskId, jobId);
            throw;
        }
    }
}
