using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartTaskManagementAPI.Application.Common.Interfaces;
using SmartTaskManagementAPI.Application.Interfaces;

namespace SmartTaskManagementAPI.Infrastructure.BackgroundJobs;

public class TaskReminderJob
{
    private readonly ILogger<TaskReminderJob> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public TaskReminderJob(ILogger<TaskReminderJob> logger, IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    [AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task SendRemindersAsync()
    {
        _logger.LogInformation("Starting task reminder job at {UtcNow}", DateTime.UtcNow);
        
        using var scope = _serviceScopeFactory.CreateScope();
        
        try
        {
            var taskRepository = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            
            // Get tasks that need reminders
            var tasksDueForReminder = await taskRepository.GetTasksDueForReminderAsync();
            
            _logger.LogInformation("Found {Count} tasks due for reminder", tasksDueForReminder.Count());

            int sentCount = 0;
            int errorCount = 0;
            
            foreach (var task in tasksDueForReminder)
            {
                try
                {
                    // Get the user who created the task
                    var user = await userRepository.GetByIdAsync(task.CreatedBy ?? Guid.Empty);
                    
                    if (user == null || !user.IsActive || user.IsDeleted)
                    {
                        _logger.LogWarning(
                            "User not found or inactive for task reminder. TaskId: {TaskId}, UserId: {UserId}",
                            task.Id, task.CreatedBy);
                        continue;
                    }

                    // Send reminder email
                    await emailService.SendTaskReminderEmailAsync(
                        user.Email,
                        user.GetFullName(),
                        task.Title,
                        task.DueDate ?? DateTime.UtcNow,
                        CancellationToken.None);
                    
                    sentCount++;
                    
                    // Log the reminder
                    _logger.LogInformation(
                        "Reminder sent for task: {TaskTitle} (ID: {TaskId}), User: {UserEmail}, Due: {DueDate}",
                        task.Title, task.Id, user.Email, task.DueDate);
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogError(ex,
                        "Failed to send reminder for task {TaskId}, UserId: {UserId}",
                        task.Id, task.CreatedBy);
                }
            }
            
            _logger.LogInformation(
                "Completed task reminder job at {UtcNow}. Sent: {SentCount}, Errors: {ErrorCount}",
                DateTime.UtcNow, sentCount, errorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while running task reminder job");
            throw; // Re-throw to trigger Hangfire retry
        }
    }
}