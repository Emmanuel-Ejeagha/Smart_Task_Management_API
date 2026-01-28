using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartTaskManagementAPI.Application.Common.Interfaces;
using SmartTaskManagementAPI.Application.Interfaces;

namespace SmartTaskManagementAPI.Infrastructure.BackgroundJobs;

public class OverdueTaskNotificationJob
{
    private readonly ILogger<OverdueTaskNotificationJob> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public OverdueTaskNotificationJob(ILogger<OverdueTaskNotificationJob> logger, IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    [AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task SendOverdueNotificationsAsync()
    {
        _logger.LogInformation("Starting overdue task notification job at {UtcNow}", DateTime.UtcNow);
        
        using var scope = _serviceScopeFactory.CreateScope();
        
        try
        {
            var taskRepository = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            
            // Get overdue tasks
            var overdueTasks = await taskRepository.GetOverdueTasksAsync();
            
            _logger.LogInformation("Found {Count} overdue tasks", overdueTasks.Count());

            int sentCount = 0;
            int errorCount = 0;
            
            foreach (var task in overdueTasks)
            {
                try
                {
                    // Get the user who created the task
                    var user = await userRepository.GetByIdAsync(task.CreatedBy ?? Guid.Empty);
                    
                    if (user == null || !user.IsActive || user.IsDeleted)
                    {
                        _logger.LogWarning(
                            "User not found or inactive for overdue notification. TaskId: {TaskId}, UserId: {UserId}",
                            task.Id, task.CreatedBy);
                        continue;
                    }

                    // Send overdue notification email
                    await emailService.SendOverdueTaskNotificationAsync(
                        user.Email,
                        user.GetFullName(),
                        task.Title,
                        task.DueDate ?? DateTime.UtcNow,
                        CancellationToken.None);
                    
                    sentCount++;
                    
                    // Log the notification
                    _logger.LogInformation(
                        "Overdue notification sent for task: {TaskTitle} (ID: {TaskId}), User: {UserEmail}, Due: {DueDate}",
                        task.Title, task.Id, user.Email, task.DueDate);
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogError(ex,
                        "Failed to send overdue notification for task {TaskId}, UserId: {UserId}",
                        task.Id, task.CreatedBy);
                }
            }
            
            _logger.LogInformation(
                "Completed overdue task notification job at {UtcNow}. Sent: {SentCount}, Errors: {ErrorCount}",
                DateTime.UtcNow, sentCount, errorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while running overdue task notification job");
            throw;
        }
    }
}