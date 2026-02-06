using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Serilog;
using SmartTaskManagementAPI.API.Middleware;
using SmartTaskManagementAPI.Infrastructure.BackgroundJobs;
using SmartTaskManagementAPI.Infrastructure.Data;
using SmartTaskManagementAPI.Infrastructure.Data.Seed;

namespace SmartTaskManagementAPI.API.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseApiConfiguration(this IApplicationBuilder app, IConfiguration configuration)
    {
        // Use Serilog for request logging
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
            options.GetLevel = (ctx, elapsed, ex) => ex != null 
                ? Serilog.Events.LogEventLevel.Error 
                : ctx.Response.StatusCode > 499 
                    ? Serilog.Events.LogEventLevel.Error 
                    : Serilog.Events.LogEventLevel.Information;
        });

        // Use custom middleware
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<TenantResolutionMiddleware>();

        // Use HTTPS Redirection in production
        if (!app.ApplicationServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        // Use Static Files
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")),
            RequestPath = "/static"
        });

        // Use Routing
        app.UseRouting();

        // Use CORS
        app.UseCors("AllowSpecificOrigin");

        // Use Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // Use Response Caching
        app.UseResponseCaching();

        // Use Endpoints
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            
            // Health check endpoint
            endpoints.MapHealthChecks("/health");
            
            // Hangfire dashboard (protected)
            endpoints.MapHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = new[] { new HangfireDashboardAuthorizationFilter() },
                DashboardTitle = "Smart Task Management Jobs",
                AppPath = "/hangfire",
                DisplayStorageConnectionString = false,
                DarkModeEnabled = true
            }).RequireAuthorization();
        });

        return app;
    }

    public static async Task<IApplicationBuilder> InitializeDatabaseAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var services = scope.ServiceProvider;
        
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            
            // Apply migrations
            await context.Database.MigrateAsync();
            
            // Seed initial data
            await DatabaseSeeder.SeedAsync(services);
            
            Log.Information("Database initialized successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while initializing the database");
            throw;
        }

        return app;
    }

    public static IApplicationBuilder InitializeRecurringJobs(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var recurringJobsService = scope.ServiceProvider.GetRequiredService<RecurringJobsService>();
        
        try
        {
            recurringJobsService.ScheduleRecurringJobs();
            Log.Information("Recurring jobs initialized successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while initializing recurring jobs");
            throw;
        }

        return app;
    }
}