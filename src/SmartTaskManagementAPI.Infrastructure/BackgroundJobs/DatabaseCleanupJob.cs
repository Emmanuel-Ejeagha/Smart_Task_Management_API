using System;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartTaskManagementAPI.Domain.Enums;
using SmartTaskManagementAPI.Infrastructure.Data;

namespace SmartTaskManagementAPI.Infrastructure.BackgroundJobs;

public class DatabaseCleanupJob
{
    private readonly ILogger<DatabaseCleanupJob> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public DatabaseCleanupJob(ILogger<DatabaseCleanupJob> logger, IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    [AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task CleanupOldDataAsync()
    {
        _logger.LogInformation("Starting database cleanup job at {UtcNow}", DateTime.UtcNow);
        
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        try
        {
            // Archive tasks that have been done for more than 30 days
            var archiveThreshold = DateTime.UtcNow.AddDays(-30);
            var tasksToArchive = await context.Tasks
                .Where(t => t.Status == TasksStatus.Done &&
                           t.UpdatedAt < archiveThreshold &&
                           !t.IsDeleted)
                .ToListAsync();
            
            foreach (var task in tasksToArchive)
            {
                task.Archive(Guid.Empty); // System user
            }
            
            _logger.LogInformation("Archived {Count} old completed tasks", tasksToArchive.Count);
            
            // Permanently delete tasks that have been soft deleted for more than 90 days
            var deleteThreshold = DateTime.UtcNow.AddDays(-90);
            var tasksToDelete = await context.Tasks
                .Where(t => t.IsDeleted && t.DeletedAt < deleteThreshold)
                .ToListAsync();
            
            context.Tasks.RemoveRange(tasksToDelete);
            
            _logger.LogInformation("Permanently deleted {Count} old soft-deleted tasks", tasksToDelete.Count);
            
            // Save all changes
            await context.SaveChangesAsync();
            
            _logger.LogInformation("Completed database cleanup job at {UtcNow}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while running database cleanup job");
            throw;
        }
    }
}